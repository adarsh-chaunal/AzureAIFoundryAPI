using System.Text.RegularExpressions;

namespace EhrClinical.AzureFunction;

public sealed class PhiScrubber
{
    // NOTE: This is a pragmatic placeholder-based scrubber.
    // It avoids emptying text; it replaces sensitive patterns with neutral tokens to preserve clinical meaning.
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

    public string Scrub(string input, string? clientNeutralName = "Client")
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var text = input;

        text = EmailRegex.Replace(text, "EmailAddress");
        text = PhoneRegex.Replace(text, "PhoneNumber");
        text = SsnRegex.Replace(text, "Identifier");
        text = MrnLikeIdRegex.Replace(text, "Identifier");

        // Dates can be clinically important. Replace with a neutral placeholder, preserving sentence structure.
        text = DateLikeRegex.Replace(text, "Date");

        // Lightweight name neutralization (best-effort). Real deployments should add an NLP/PII detector.
        if (!string.IsNullOrWhiteSpace(clientNeutralName))
        {
            text = ReplaceObviousClientNameMentions(text, clientNeutralName!);
        }

        return text;
    }

    private static string ReplaceObviousClientNameMentions(string text, string placeholder)
    {
        // Replace "Patient:"/"Client:" prefixes and common narrative forms.
        text = Regex.Replace(text, @"\b(Patient|Client)\s*Name\s*[:\-]\s*.+?$", $"$1 Name: {placeholder}",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Replace "Mr./Ms./Mrs. <LastName>" with Client
        text = Regex.Replace(text, @"\b(Mr|Ms|Mrs|Miss|Mx)\.\s+[A-Z][a-zA-Z'\-]{1,}\b", placeholder,
            RegexOptions.Compiled);

        return text;
    }
}

