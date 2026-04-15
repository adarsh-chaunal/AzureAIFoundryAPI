using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("EhrClinical");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing connection string 'EhrClinical'. Set it in appsettings.json or environment variables.");
    return 1;
}

var migrationsRoot = Path.Combine(AppContext.BaseDirectory, "Migrations");
if (!Directory.Exists(migrationsRoot))
{
    Console.Error.WriteLine($"Migrations folder not found at '{migrationsRoot}'.");
    return 1;
}

try
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync().ConfigureAwait(false);

    await EnsureJournalAsync(connection).ConfigureAwait(false);

    var applied = await GetAppliedScriptNamesAsync(connection).ConfigureAwait(false);

    var scripts = Directory.EnumerateFiles(migrationsRoot, "*.sql", SearchOption.AllDirectories)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToList();

    foreach (var scriptPath in scripts)
    {
        var scriptName = Path.GetRelativePath(migrationsRoot, scriptPath).Replace('\\', '/');
        if (applied.Contains(scriptName))
        {
            Console.WriteLine($"Skip (already applied): {scriptName}");
            continue;
        }

        var sqlText = await File.ReadAllTextAsync(scriptPath).ConfigureAwait(false);
        Console.WriteLine($"Apply: {scriptName}");

        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var batch in SplitSqlBatches(sqlText))
            {
                await using var command = new SqlCommand(batch, connection, transaction);
                command.CommandTimeout = 0;
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await using (var logCommand = new SqlCommand(
                             "INSERT INTO dbo.DatabaseMigrationJournal (ScriptName) VALUES (@ScriptName);",
                             connection,
                             transaction))
            {
                logCommand.Parameters.AddWithValue("@ScriptName", scriptName);
                await logCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            transaction.Commit();
            Console.WriteLine($"Done: {scriptName}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.Error.WriteLine($"Failed applying '{scriptName}': {ex.Message}");
            return 1;
        }
    }

    Console.WriteLine("All pending migrations are applied.");
    return 0;
}
catch (SqlException ex) when (ex.Number is 4060 or 18456)
{
    Console.Error.WriteLine(
        "Could not open the SQL catalog. Create it first (see Database/Prerequisites/CreateClinicalCatalog.sql) or update the connection string.");
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static async Task EnsureJournalAsync(SqlConnection connection)
{
    const string sql = """
        IF OBJECT_ID(N'dbo.DatabaseMigrationJournal', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.DatabaseMigrationJournal
            (
                JournalKey    BIGINT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_DatabaseMigrationJournal PRIMARY KEY,
                ScriptName    NVARCHAR(260)          NOT NULL CONSTRAINT UQ_DatabaseMigrationJournal_Script UNIQUE,
                AppliedAtUtc  DATETIME2              NOT NULL CONSTRAINT DF_DatabaseMigrationJournal_Applied DEFAULT (SYSUTCDATETIME())
            );
        END
        """;

    await using var command = new SqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
}

static async Task<HashSet<string>> GetAppliedScriptNamesAsync(SqlConnection connection)
{
    const string sql = "SELECT ScriptName FROM dbo.DatabaseMigrationJournal;";
    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync().ConfigureAwait(false))
    {
        names.Add(reader.GetString(0));
    }

    return names;
}

static IEnumerable<string> SplitSqlBatches(string script)
{
    var builder = new StringBuilder();
    foreach (var line in script.Split(["\r\n", "\n"], StringSplitOptions.None))
    {
        if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
        {
            var batch = builder.ToString().Trim();
            builder.Clear();
            if (batch.Length > 0)
            {
                yield return batch;
            }
        }
        else
        {
            builder.AppendLine(line);
        }
    }

    var last = builder.ToString().Trim();
    if (last.Length > 0)
    {
        yield return last;
    }
}
