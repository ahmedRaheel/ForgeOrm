using ForgeORM.Core.Graph;
using ForgeORM.Core.Graph.Strategies;

namespace ForgeORM.Providers.SqlServer.Graph;

/// <summary>
/// SQL Server graph executor that routes graph nodes to OPENJSON, TVP, SqlBulkCopy, or MERGE strategies.
/// </summary>
public sealed class SqlServerForgeGraphExecutor : IForgeGraphExecutor
{
    private readonly IForgeGraphStrategySelector _selector;
    private readonly IForgeForeignKeyBinder _foreignKeyBinder;
    private readonly ForgeGraphIdentityMap _identityMap = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerForgeGraphExecutor"/> class.
    /// </summary>
    public SqlServerForgeGraphExecutor(IForgeGraphStrategySelector selector, IForgeForeignKeyBinder foreignKeyBinder)
    {
        _selector = selector;
        _foreignKeyBinder = foreignKeyBinder;
    }

    /// <inheritdoc />
    public ForgeDatabaseProvider Provider => ForgeDatabaseProvider.SqlServer;

    /// <inheritdoc />
    public async ValueTask<ForgeGraphResult> InsertGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        var result = new ForgeGraphResultBuilder();
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Insert, options);

        foreach (var node in plan.GetInsertOrder())
        {
            BindParentKeys(node);
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Insert, node.Rows.Count, options);
            await ExecuteInsertNodeAsync(node, strategy, cancellationToken).ConfigureAwait(false);
            CaptureGeneratedKeys(node);
            result.AddInserted(node.TableName, node.Rows.Count, strategy);
        }

        return result.Build();
    }

    /// <inheritdoc />
    public async ValueTask<ForgeGraphResult> UpdateGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        var result = new ForgeGraphResultBuilder();
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Update, options);

        foreach (var node in plan.GetInsertOrder())
        {
            BindParentKeys(node);
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Update, node.Rows.Count, options);
            await ExecuteMergeNodeAsync(node, strategy, options, cancellationToken).ConfigureAwait(false);
            result.AddUpdated(node.TableName, node.Rows.Count, strategy);
        }

        return result.Build();
    }

    /// <inheritdoc />
    public async ValueTask<ForgeGraphResult> DeleteGraphAsync<T>(T entity, ForgeGraphOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        var result = new ForgeGraphResultBuilder();
        var plan = ForgeGraphPlanBuilder.Build(entity, ForgeGraphOperation.Delete, options);

        foreach (var node in plan.GetDeleteOrder())
        {
            var strategy = _selector.Select(Provider, ForgeGraphOperation.Delete, node.Rows.Count, options);
            await ExecuteDeleteNodeAsync(node, strategy, options, cancellationToken).ConfigureAwait(false);
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
            var key = ForgeProviderAccessors.Get(metadata.KeyProperty, row!);
            _identityMap.SetDatabaseKey(row, key);
        }
    }

    private static ValueTask ExecuteInsertNodeAsync(ForgeGraphNode node, ForgeBulkStrategy strategy, CancellationToken cancellationToken)
    {
        // SQL Server implementation hook:
        // OpenJson: INSERT ... SELECT FROM OPENJSON(@json) WITH (...)
        // TVP: INSERT ... SELECT FROM @TableType
        // SqlBulkCopy: bulk copy rows into target or staging table
        return ValueTask.CompletedTask;
    }

    private static ValueTask ExecuteMergeNodeAsync(ForgeGraphNode node, ForgeBulkStrategy strategy, ForgeGraphOptions options, CancellationToken cancellationToken)
    {
        // SQL Server implementation hook:
        // MERGE target USING source
        // WHEN MATCHED UPDATE
        // WHEN NOT MATCHED INSERT
        // WHEN NOT MATCHED BY SOURCE DELETE when options.ChildSyncMode requires it
        return ValueTask.CompletedTask;
    }

    private static ValueTask ExecuteDeleteNodeAsync(ForgeGraphNode node, ForgeBulkStrategy strategy, ForgeGraphOptions options, CancellationToken cancellationToken)
    {
        // SQL Server implementation hook:
        // SoftDelete: UPDATE target SET IsDeleted = 1, DeletedAt = SYSUTCDATETIME()
        // HardDelete: DELETE FROM target WHERE Id IN (...)
        return ValueTask.CompletedTask;
    }
}
