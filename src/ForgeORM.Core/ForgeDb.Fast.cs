using ForgeORM.Abstractions;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Fast primary-key lookup path. Bypasses expression translation, include handling, graph logic, and query-state rendering.
    /// This is the path to benchmark against Dapper QueryFirst/QueryFirstOrDefault by primary key.
    /// </summary>
    public T? Find<T>(object id, int? timeoutSeconds = null)
    {
        var metadata = _metadata.Resolve<T>();
        var c = Provider.BuildGetById(metadata, id);
        return ForgeFrameworkExecutionPolicy.FirstOrDefault<T, ForgeIdParameter<object?>>(Provider, _connectionString, c.CommandText, ForgeIdParameter<object?>.Create(id), timeoutSeconds);
    }

    /// <summary>
    /// Fast primary-key lookup path. Bypasses expression translation, include handling, graph logic, and query-state rendering.
    /// This is the path to benchmark against Dapper QueryFirst/QueryFirstOrDefault by primary key.
    /// </summary>
    public async Task<T?> FindAsync<T>(object id, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        var c = Provider.BuildGetById(metadata, id);
        return await ForgeFrameworkExecutionPolicy.FirstOrDefaultAsync<T, ForgeIdParameter<object?>>(Provider, _connectionString, c.CommandText, ForgeIdParameter<object?>.Create(id), timeoutSeconds, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fast raw SQL list path. Skips expression translation and navigation processing.
    /// </summary>
    public IReadOnlyList<T> QueryFast<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        return ForgeFrameworkExecutionPolicy.Query<T>(Provider, _connectionString, sql, parameters, timeoutSeconds);
    }

    /// <summary>
    /// Fast raw SQL list path. Skips expression translation and navigation processing.
    /// </summary>
    public async Task<IReadOnlyList<T>> QueryFastAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        return await ForgeFrameworkExecutionPolicy.QueryAsync<T>(Provider, _connectionString, sql, parameters, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Fast raw SQL single-row path. Skips expression translation and navigation processing.
    /// </summary>
    public T? QueryFirstFast<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        return ForgeFrameworkExecutionPolicy.FirstOrDefault<T>(Provider, _connectionString, sql, parameters, timeoutSeconds);
    }

    /// <summary>
    /// Fast raw SQL single-row path. Skips expression translation and navigation processing.
    /// </summary>
    public async Task<T?> QueryFirstFastAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        return await ForgeFrameworkExecutionPolicy.FirstOrDefaultAsync<T>(Provider, _connectionString, sql, parameters, timeoutSeconds, cancellationToken);
    }

}
