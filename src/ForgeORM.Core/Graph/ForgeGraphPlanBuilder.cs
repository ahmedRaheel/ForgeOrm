namespace ForgeORM.Core.Graph;

/// <summary>
/// Builds dependency-aware graph persistence plans.
/// </summary>
public static class ForgeGraphPlanBuilder
{
    /// <summary>
    /// Builds a graph plan for a single root entity.
    /// </summary>
    public static ForgeGraphPlan Build<T>(
        T entity,
        ForgeGraphOperation operation,
        ForgeGraphOptions options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(options);

        var plan = new ForgeGraphPlan { Operation = operation };
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

        Visit(entity, parent: null, depth: 0, plan, options, visited);

        return plan;
    }

    private static void Visit(
        object entity,
        object? parent,
        int depth,
        ForgeGraphPlan plan,
        ForgeGraphOptions options,
        HashSet<object> visited)
    {
        if (depth > options.MaxDepth || !visited.Add(entity))
        {
            return;
        }

        var metadata = ForgeEntityMetadataCache.Get(entity.GetType());
        var node = GetOrCreateNode(plan, metadata, depth, parent);
        node.Rows.Add(entity);

        if (parent is not null)
        {
            node.ParentByChild[entity] = parent;
        }

        if (!options.IncludeChildren || options.ChildSyncMode == ForgeChildSyncMode.IgnoreChildren)
        {
            return;
        }

        foreach (var collectionProperty in metadata.ChildCollections)
        {
            if (collectionProperty.GetValue(entity) is not System.Collections.IEnumerable children)
            {
                continue;
            }

            foreach (var child in children)
            {
                if (child is null)
                {
                    continue;
                }

                Visit(child, entity, depth + 1, plan, options, visited);
            }
        }
    }

    private static ForgeGraphNode GetOrCreateNode(
        ForgeGraphPlan plan,
        ForgeEntityMetadata metadata,
        int depth,
        object? parent)
    {
        var node = plan.Nodes.FirstOrDefault(x => x.EntityType == metadata.EntityType);
        if (node is not null)
        {
            return node;
        }

        var newNode = new ForgeGraphNode
        {
            TableName = metadata.TableName,
            EntityType = metadata.EntityType,
            Depth = depth
        };

        if (parent is not null)
        {
            var parentMetadata = ForgeEntityMetadataCache.Get(parent.GetType());
            newNode.ParentType = parent.GetType();
            newNode.ParentKeyProperty = parentMetadata.KeyProperty;
            newNode.ForeignKeyProperty = parentMetadata.KeyProperty is null
                ? null
                : ForgeForeignKeyBinder.FindForeignKey(parent.GetType(), metadata.EntityType, parentMetadata.KeyProperty.Name);
        }

        plan.Nodes.Add(newNode);
        return newNode;
    }
}
