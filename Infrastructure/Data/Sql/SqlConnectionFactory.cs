using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data.Sql;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        //_connectionString = ResolveConnectionString(configuration);
        _connectionString =
            configuration.GetConnectionString("EhrClinical")
            ?? configuration["ConnectionStrings:EhrClinical"]
            ?? configuration["Sql:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Connection string is not configured. Set either ConnectionStrings:EhrClinical (preferred) or Sql:ConnectionString.");
    }

    //private static string ResolveConnectionString(IConfiguration configuration)
    //{
    //    var inContainer = string.Equals(
    //        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    //        "true",
    //        StringComparison.OrdinalIgnoreCase);

    //    var explicitDocker = configuration.GetConnectionString("EhrClinicalDocker");
    //    if (inContainer && !string.IsNullOrWhiteSpace(explicitDocker))
    //    {
    //        return explicitDocker;
    //    }

    //    var primary = configuration.GetConnectionString("EhrClinical")
    //        ?? throw new InvalidOperationException("Connection string 'EhrClinical' is not configured.");

    //    if (!inContainer)
    //    {
    //        return primary;
    //    }

    //    var builder = new SqlConnectionStringBuilder(primary)
    //    {
    //        DataSource = configuration["Database:DockerSqlHost"] ?? "host.docker.internal,1433",
    //        IntegratedSecurity = false
    //    };

    //    var user = configuration["Database:DockerUser"];
    //    var password = configuration["Database:DockerPassword"];
    //    if (string.IsNullOrWhiteSpace(user))
    //    {
    //        throw new InvalidOperationException(
    //            "The API is running in a container, so the SQL host from your machine (for example a Windows computer name) cannot be resolved. " +
    //            "Either set ConnectionStrings:EhrClinicalDocker to a full connection string that reaches SQL Server on the host (typically Server=host.docker.internal,1433;...), " +
    //            "or keep ConnectionStrings:EhrClinical for local development and set Database:DockerUser and Database:DockerPassword (SQL authentication). " +
    //            "Optional Database:DockerSqlHost defaults to host.docker.internal,1433. " +
    //            "Windows integrated security is not used from Linux containers.");
    //    }

    //    builder.UserID = user;
    //    builder.Password = password ?? string.Empty;

    //    builder.TrustServerCertificate = true;
    //    builder.Encrypt = false;

    //    return builder.ConnectionString;
    //}

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
