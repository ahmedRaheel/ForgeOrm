using ForgeORM.Core.Graph;
using ForgeORM.Core.Graph.Strategies;

namespace ForgeORM.Providers.Oracle.Graph;

/// <summary>
/// Oracle graph executor that routes graph nodes to array binding, MERGE, and global temporary table strategies.
/// </summary>
public sealed class OracleForgeGraphExecutor : IForgeGraphExecutor
{
    private readonly IForgeGraphStrategySelector _selector;
    private readonly IForgeForeignKeyBinder _foreignKeyBinder;
    private readonly ForgeGraphIdentityMap _identityMap = new();

    public OracleForgeGraphExecutor(IForgeGraphStrategySelector selector, IForgeForeignKeyBinder foreignKeyBinder)
    {
        _selector = selector;
        _foreignKeyBinder = foreignKeyBinder;
    }

    public ForgeDatabaseProvider Provider => ForgeDatabaseProvider.Oracle;

    public async ValueTask<ForgeGraphResult> InsertGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default) where T : class
    {
        var result = new ForgeGraphResultBuilder();
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Insert, options);
        foreach (var node in plan.GetInsertOrder())
        {
            BindParentKeys(node);
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Insert, node.Rows.Count, options);
            await ExecuteNodeAsync(node, strategy, options, cancellationToken).ConfigureAwait(false);
            CaptureGeneratedKeys(node);
            result.AddInserted(node.TableName, node.Rows.Count, strategy);
        }
        return result.Build();
    }

    public async ValueTask<ForgeGraphResult> UpdateGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default) where T : class
    {
        var result = new ForgeGraphResultBuilder();
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Update, options);
        foreach (var node in plan.GetInsertOrder())
        {
            BindParentKeys(node);
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Update, node.Rows.Count, options);
            await ExecuteNodeAsync(node, strategy, options, cancellationToken).ConfigureAwait(false);
            result.AddUpdated(node.TableName, node.Rows.Count, strategy);
        }
        return result.Build();
    }

    public async ValueTask<ForgeGraphResult> DeleteGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default) where T : class
    {
        var result = new ForgeGraphResultBuilder();
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Delete, options);
        foreach (var node in plan.GetDeleteOrder())
        {
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Delete, node.Rows.Count, options);
            await ExecuteNodeAsync(node, strategy, options, cancellationToken).ConfigureAwait(false);
            result.AddDeleted(node.TableName, node.Rows.Count, strategy);
        }
        return result.Build();
    }

    private void BindParentKeys(ForgeGraphNode node)
    {
        foreach (var row in node.Rows)
        {
            if (node.ParentByChild.TryGetValue(row, out var parent))
            {
                _foreignKeyBinder.Bind(parent, row, _identityMap);
            }
        }
    }

    private void CaptureGeneratedKeys(ForgeGraphNode node)
    {
        foreach (var row in node.Rows)
        {
            var metadata = ForgeEntityMetadataCache.Get(row.GetType());
            _identityMap.SetDatabaseKey(row, ForgeProviderAccessors.Get(metadata.KeyProperty, row!));
        }
    }

    private static ValueTask ExecuteNodeAsync(ForgeGraphNode node, ForgeBulkStrategy strategy, ForgeGraphOptions options, CancellationToken cancellationToken)
    {
        // Oracle hook: array binding, RETURNING INTO, MERGE, global temporary table.
        return ValueTask.CompletedTask;
    }
}
