using Microsoft.Data.SqlClient;

namespace Infrastructure.Data.Sql;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
