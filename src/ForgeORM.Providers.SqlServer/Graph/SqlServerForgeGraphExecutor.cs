using ForgeORM.Core.Graph;
using ForgeORM.Core.Graph.Strategies;

namespace ForgeORM.SqlServer.Graph;

/// <summary>
/// Compatibility SQL Server graph executor wrapper skeleton.
/// Prefer ForgeORM.Providers.SqlServer.Graph.SqlServerForgeGraphExecutor for provider package usage.
/// </summary>
public sealed class SqlServerForgeGraphExecutor : IForgeGraphExecutor
{
    private readonly IForgeGraphStrategySelector _selector;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerForgeGraphExecutor"/> class.
    /// </summary>
    public SqlServerForgeGraphExecutor(IForgeGraphStrategySelector selector)
    {
        _selector = selector;
    }

    /// <inheritdoc />
    public ForgeDatabaseProvider Provider => ForgeDatabaseProvider.SqlServer;

    /// <inheritdoc />
    public Task<ForgeGraphResult> InsertGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Insert, options);
        var result = new ForgeGraphResultBuilder();

        foreach (var node in plan.GetInsertOrder())
        {
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Insert, node.Rows.Count, options);
            result.AddInserted(node.TableName, node.Rows.Count, strategy);
        }

        return Task.FromResult(result.Build());
    }

    /// <inheritdoc />
    public Task<ForgeGraphResult> UpdateGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Update, options);
        var result = new ForgeGraphResultBuilder();

        foreach (var node in plan.GetInsertOrder())
        {
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Update, node.Rows.Count, options);
            result.AddUpdated(node.TableName, node.Rows.Count, strategy);
        }

        return Task.FromResult(result.Build());
    }

    /// <inheritdoc />
    public Task<ForgeGraphResult> DeleteGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Delete, options);
        var result = new ForgeGraphResultBuilder();

        foreach (var node in plan.GetDeleteOrder())
        {
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Delete, node.Rows.Count, options);
            result.AddDeleted(node.TableName, node.Rows.Count, strategy);
        }

        return Task.FromResult(result.Build());
    }
}
