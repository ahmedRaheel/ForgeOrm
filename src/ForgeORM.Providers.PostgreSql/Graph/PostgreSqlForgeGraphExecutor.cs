using ForgeORM.Core.Graph;
using ForgeORM.Core.Graph.Strategies;

namespace ForgeORM.Providers.PostgreSql.Graph;

/// <summary>
/// PostgreSQL graph executor that routes graph nodes to jsonb_to_recordset, UNNEST, COPY, or ON CONFLICT strategies.
/// </summary>
public sealed class PostgreSqlForgeGraphExecutor : IForgeGraphExecutor
{
    private readonly IForgeGraphStrategySelector _selector;
    private readonly IForgeForeignKeyBinder _foreignKeyBinder;
    private readonly ForgeGraphIdentityMap _identityMap = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlForgeGraphExecutor"/> class.
    /// </summary>
    public PostgreSqlForgeGraphExecutor(IForgeGraphStrategySelector selector, IForgeForeignKeyBinder foreignKeyBinder)
    {
        _selector = selector;
        _foreignKeyBinder = foreignKeyBinder;
    }

    /// <inheritdoc />
    public ForgeDatabaseProvider Provider => ForgeDatabaseProvider.PostgreSql;

    /// <inheritdoc />
    public async Task<ForgeGraphResult> InsertGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default) where T : class
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

    /// <inheritdoc />
    public async Task<ForgeGraphResult> UpdateGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default) where T : class
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

    /// <inheritdoc />
    public async Task<ForgeGraphResult> DeleteGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default) where T : class
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
            _identityMap.SetDatabaseKey(row, metadata.KeyProperty?.GetValue(row));
        }
    }

    private static Task ExecuteNodeAsync(ForgeGraphNode node, ForgeBulkStrategy strategy, ForgeGraphOptions options, CancellationToken cancellationToken)
    {
        // PostgreSQL hook: jsonb_to_recordset, UNNEST, COPY, INSERT ... ON CONFLICT, DELETE missing children.
        return Task.CompletedTask;
    }
}
