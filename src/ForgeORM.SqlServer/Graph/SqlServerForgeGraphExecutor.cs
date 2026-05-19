using ForgeORM.Core.Graph.Strategies;

namespace ForgeORM.SqlServer.Graph;

/// <summary>
/// SQL Server graph executor.
/// </summary>
public sealed class SqlServerForgeGraphExecutor : IForgeGraphExecutor
{
    private readonly IForgeGraphStrategySelector _selector;

    public SqlServerForgeGraphExecutor(
        IForgeGraphStrategySelector selector)
    {
        _selector = selector;
    }

    public ForgeDatabaseProvider Provider => ForgeDatabaseProvider.SqlServer;

    public Task InsertGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var strategy = _selector.Select(
            Provider,
            ForgeGraphOperation.Insert,
            100,
            options);

        return strategy switch
        {
            ForgeBulkStrategy.OpenJson => InsertUsingOpenJsonAsync(entity, cancellationToken),
            ForgeBulkStrategy.TableValuedParameter => InsertUsingTvpAsync(entity, cancellationToken),
            ForgeBulkStrategy.SqlBulkCopy => InsertUsingBulkCopyAsync(entity, cancellationToken),
            _ => InsertRowByRowAsync(entity, cancellationToken)
        };
    }

    public Task UpdateGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.CompletedTask;
    }

    public Task DeleteGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.CompletedTask;
    }

    private Task InsertUsingOpenJsonAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task InsertUsingTvpAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task InsertUsingBulkCopyAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task InsertRowByRowAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}