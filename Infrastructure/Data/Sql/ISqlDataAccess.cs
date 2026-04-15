namespace Infrastructure.Data.Sql;

public interface ISqlDataAccess
{
    Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
}
