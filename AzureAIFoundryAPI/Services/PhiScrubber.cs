using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AzureAIFoundryAPI.Services;

public sealed class PhiScrubber
{
    private static readonly Regex EmailRegex = new(
        @"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"(?<!\d)(?:\+?1[\s\-\.]?)?(?:\(\s*\d{3}\s*\)|\d{3})[\s\-\.]?\d{3}[\s\-\.]?\d{4}(?!\d)",
        RegexOptions.Compiled);

    private static readonly Regex SsnRegex = new(
        @"(?<!\d)\d{3}\-?\d{2}\-?\d{4}(?!\d)",
        RegexOptions.Compiled);

    private static readonly Regex MrnLikeIdRegex = new(
        @"\b(?:MRN|Member\s*ID|Patient\s*ID)\s*[:#]?\s*[A-Z0-9\-]{5,}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DateLikeRegex = new(
        @"\b(?:\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}|\d{4}[\/\-]\d{1,2}[\/\-]\d{1,2})\b",
        RegexOptions.Compiled);

    private readonly IConfiguration _configuration;
    private readonly ILogger<PhiScrubber> _logger;

    public PhiScrubber(IConfiguration configuration, ILogger<PhiScrubber> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PhiScrubResult> ScrubAsync(
        string input,
        string? providerOverride,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new PhiScrubResult(string.Empty, "None", false, null);
        }

        var provider = string.IsNullOrWhiteSpace(providerOverride)
            ? _configuration["PhiRedaction:Provider"] ?? "Regex"
            : providerOverride;

        if (provider.Equals("Regex", StringComparison.OrdinalIgnoreCase))
        {
            return new PhiScrubResult(ScrubWithRegex(input), "Regex", false, null);
        }

        if (!provider.Equals("Presidio", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unsupported PHI redaction provider. Use 'Regex' or 'Presidio'.", nameof(providerOverride));
        }

        try
        {
            var presidioText = await ScrubWithPresidioAsync(input, cancellationToken).ConfigureAwait(false);
            return new PhiScrubResult(presidioText, "Presidio", false, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failClosed =
                providerOverride?.Equals("Presidio", StringComparison.OrdinalIgnoreCase) == true
                || bool.TryParse(_configuration["PhiRedaction:FailOnPresidioError"], out var parsed) && parsed;
            if (failClosed)
            {
                throw new InvalidOperationException("Presidio redaction failed before data left the API process.", ex);
            }

            _logger.LogWarning(ex, "Presidio redaction failed. Falling back to local regex PHI scrubber.");
            return new PhiScrubResult(ScrubWithRegex(input), "Regex", true, ex.Message);
        }
    }

    private static string ScrubWithRegex(string input)
    {
        var text = input;
        text = EmailRegex.Replace(text, "EmailAddress");
        text = PhoneRegex.Replace(text, "PhoneNumber");
        text = SsnRegex.Replace(text, "Identifier");
        text = MrnLikeIdRegex.Replace(text, "Identifier");
        text = DateLikeRegex.Replace(text, "Date");
        text = Regex.Replace(text, @"\b((?:Patient|Client|Member)?\s*Name\s*[:\-]\s*)[A-Z][A-Za-z'\-]+(?:\s+[A-Z][A-Za-z'\-]+){0,4}(?=\s*[\.;,\r\n]|$)", "$1Client",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        text = Regex.Replace(text, @"\b(Mr|Ms|Mrs|Miss|Mx)\.\s+[A-Z][a-zA-Z'\-]{1,}\b", "Client",
            RegexOptions.Compiled);
        return text;
    }

    private async Task<string> ScrubWithPresidioAsync(string input, CancellationToken cancellationToken)
    {
        var pythonExecutable = ResolveConfiguredPath(
            _configuration["PhiRedaction:PresidioPythonExecutable"],
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3");
        var scriptPath = ResolveConfiguredPath(
            _configuration["PhiRedaction:PresidioScriptPath"],
            Path.Combine(AppContext.BaseDirectory, "Scripts", "presidio_redact.py"));
        var timeoutSeconds = int.TryParse(_configuration["PhiRedaction:PresidioTimeoutSeconds"], out var parsedTimeout)
            ? parsedTimeout
            : 30;

        var request = JsonSerializer.Serialize(new PresidioRedactionRequest(
            input,
            _configuration["PhiRedaction:PresidioLanguage"] ?? "en",
            ReadConfiguredEntities(),
            _configuration["PhiRedaction:PresidioSpacyModel"] ?? "en_core_web_sm"));

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add(scriptPath);

        if (!process.Start())
        {
            throw new InvalidOperationException("Could not start local Presidio redaction process.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.StandardInput.WriteAsync(request.AsMemory(), cancellationToken).ConfigureAwait(false);
            process.StandardInput.Close();
        }
        catch (IOException ex)
        {
            var earlyOutput = await outputTask.ConfigureAwait(false);
            var earlyError = await errorTask.ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Presidio redaction process closed stdin before reading input. Python='{pythonExecutable}', Script='{scriptPath}', StdOut='{earlyOutput}', StdErr='{earlyError}'",
                ex);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException($"Presidio redaction exceeded {timeoutSeconds} seconds.");
        }

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);

        PresidioRedactionResponse? response = null;
        if (!string.IsNullOrWhiteSpace(output))
        {
            response = JsonSerializer.Deserialize<PresidioRedactionResponse>(output);
        }

        if (process.ExitCode != 0)
        {
            var details = !string.IsNullOrWhiteSpace(response?.Error) ? response.Error : error;
            throw new InvalidOperationException($"Presidio redaction process failed: {details}");
        }

        if (!string.IsNullOrWhiteSpace(response?.Error))
        {
            throw new InvalidOperationException(response.Error);
        }

        return response?.RedactedText ?? string.Empty;
    }

    private static string ResolveConfiguredPath(string? configuredPath, string defaultPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return defaultPath;
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var contentRootPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
        if (File.Exists(contentRootPath))
        {
            return contentRootPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    private string[]? ReadConfiguredEntities()
    {
        var configuredEntities = _configuration["PhiRedaction:PresidioEntities"];
        if (string.IsNullOrWhiteSpace(configuredEntities))
        {
            return null;
        }

        return configuredEntities
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Nothing useful to do if the process already exited between checks.
        }
    }

    private sealed record PresidioRedactionRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("entities")] string[]? Entities,
        [property: JsonPropertyName("spacy_model")] string SpacyModel);

    private sealed record PresidioRedactionResponse(
        [property: JsonPropertyName("redacted_text")] string? RedactedText,
        [property: JsonPropertyName("error")] string? Error);
}

public sealed record PhiScrubResult(
    string Text,
    string Provider,
    bool UsedFallback,
    string? Error);
