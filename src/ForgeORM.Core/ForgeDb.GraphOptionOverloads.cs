using System.Collections;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Core.Graph;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Updates a graph using enterprise graph options. When ChildSyncMode is InsertUpdateDeleteMissing,
    /// missing child rows are deleted after child synchronization.
    /// </summary>
    public Task<int> UpdateGraphAsync<T>(
        T entity,
        Action<ForgeGraphOptions> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ForgeGraphOptions();
        configure(options);

        var deleteMissingChildren = options.IncludeChildren &&
            options.ChildSyncMode == ForgeChildSyncMode.InsertUpdateDeleteMissing;

        return UpdateGraphAsync(entity, deleteMissingChildren, cancellationToken);
    }

    /// <summary>
    /// Deletes a graph by id using enterprise graph options. Hard delete removes children first.
    /// Soft delete updates the configured soft-delete column on children and parent.
    /// </summary>
    public async Task<int> DeleteGraphAsync<T>(
        object id,
        Action<ForgeGraphOptions> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ForgeGraphOptions();
        configure(options);

        if (options.DeleteMode == ForgeDeleteMode.SoftDelete)
        {
            return await SoftDeleteGraphAsync<T>(id, options, cancellationToken);
        }

        if (options.IncludeChildren)
        {
            return await DeleteGraphAsync<T>(id, cancellationToken);
        }

        return await DeleteParentOnlyAsync<T>(id, cancellationToken);
    }

    /// <summary>
    /// Deletes a graph using the key value from the provided entity instance.
    /// </summary>
    public Task<int> DeleteGraphAsync<T>(
        T entity,
        Action<ForgeGraphOptions> configure,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        var id = GetEntityKeyValue(entity);
        return DeleteGraphAsync<T>(id, configure, cancellationToken);
    }

    /// <summary>
    /// Deletes a graph using the key value from the provided entity instance.
    /// </summary>
    public Task<int> DeleteGraphAsync<T>(
        T entity,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        var id = GetEntityKeyValue(entity);
        return DeleteGraphAsync<T>(id, cancellationToken);
    }

    private async Task<int> DeleteParentOnlyAsync<T>(
        object id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parentCommand = Provider.BuildDelete(_metadata.Resolve<T>(), id);
        return await ForgeAdo.ExecuteAsync(
            connection,
            parentCommand.CommandText,
            parentCommand.Parameters,
            commandType: parentCommand.CommandType,
            timeoutSeconds: parentCommand.TimeoutSeconds,
            cancellationToken: cancellationToken);
    }

    private async Task<int> SoftDeleteGraphAsync<T>(
        object id,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var affected = 0;

            if (options.IncludeChildren)
            {
                foreach (var collection in GetGraphChildCollectionProperties(typeof(T)))
                {
                    var childType = GetGraphCollectionItemType(collection.PropertyType);
                    if (childType is null)
                    {
                        continue;
                    }

                    var fk = FindGraphForeignKeyProperty(childType, typeof(T));
                    if (fk is null)
                    {
                        continue;
                    }

                    var childShape = ForgeEntityShape.For(childType);
                    var fkColumn = ForgeEntityShape.ColumnName(fk);
                    var childSql = $"UPDATE {childShape.TableName} SET {options.SoftDeleteColumn} = 1 WHERE {fkColumn} = @ParentId";

                    affected += await ForgeAdo.ExecuteAsync(
                        connection,
                        childSql,
                        new Dictionary<string, object?> { ["ParentId"] = id },
                        transaction,
                        cancellationToken: cancellationToken);
                }
            }

            var parentShape = ForgeEntityShape.For(typeof(T));
            var parentKey = parentShape.KeyProperty
                ?? throw new InvalidOperationException($"Entity '{typeof(T).Name}' does not have a key property configured.");

            var parentSql = $"UPDATE {parentShape.TableName} SET {options.SoftDeleteColumn} = 1 WHERE {ForgeEntityShape.ColumnName(parentKey)} = @Id";

            affected += await ForgeAdo.ExecuteAsync(
                connection,
                parentSql,
                new Dictionary<string, object?> { ["Id"] = id },
                transaction,
                cancellationToken: cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static object GetEntityKeyValue<T>(T entity)
        where T : class
    {
        var shape = ForgeEntityShape.For(typeof(T));
        var key = shape.KeyProperty
            ?? throw new InvalidOperationException($"Entity '{typeof(T).Name}' does not have a key property configured.");

        var value = ForgeRuntimeAccessorCache.Get(key, entity);
        if (value is null)
        {
            throw new InvalidOperationException($"Entity '{typeof(T).Name}' key '{key.Name}' cannot be null.");
        }

        return value;
    }

    private static IReadOnlyList<PropertyInfo> GetGraphChildCollectionProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && GetGraphCollectionItemType(p.PropertyType) is not null)
            .ToList();

    private static Type? GetGraphCollectionItemType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && type.GetGenericArguments().Length == 1)
        {
            return type.GetGenericArguments()[0];
        }

        return type.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static PropertyInfo? FindGraphForeignKeyProperty(Type childType, Type parentType)
    {
        var parentName = parentType.Name;
        return childType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name.Equals(parentName + "Id", StringComparison.OrdinalIgnoreCase))
            ?? childType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && p.Name.Contains(parentName, StringComparison.OrdinalIgnoreCase));
    }
}
