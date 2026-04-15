using Dapper;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data.Sql;

public sealed class SqlDataAccess : ISqlDataAccess
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlDataAccess(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<T>(command).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
    }
}
