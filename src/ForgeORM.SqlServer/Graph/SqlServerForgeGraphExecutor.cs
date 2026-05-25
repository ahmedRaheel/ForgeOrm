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

    public ValueTask InsertGraphAsync<T>(
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

    public ValueTask UpdateGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return ValueTask.CompletedTask;
    }

    private ValueTask InsertUsingOpenJsonAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private ValueTask InsertUsingTvpAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private ValueTask InsertUsingBulkCopyAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private ValueTask InsertRowByRowAsync<T>(T entity, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}