using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Inserts multiple independent entities in one transaction.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of inserted rows.</returns>
    public async Task<int> InsertManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var rows = entities.Where(x => x is not null).ToList();
        if (rows.Count == 0) return 0;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var affected = 0;
            var metadata = _metadata.Resolve<T>();
            foreach (var row in rows)
            {
                var command = Provider.BuildInsert(metadata, row!);
                affected += await ForgeAdo.ExecuteAsync(
                    connection,
                    command.CommandText,
                    command.Parameters,
                    transaction,
                    command.CommandType,
                    command.TimeoutSeconds,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Inserts a parent entity and every public child collection automatically in one transaction.
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <param name="entity">The aggregate root containing child collection properties.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The same entity instance after key propagation.</returns>
    public async Task<T> InsertGraphAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await InsertGraphNodeAsync(connection, transaction, entity!, parentKeyValue: null, parentType: null, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return entity;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Updates a parent entity and synchronizes child collections in one transaction.
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <param name="entity">The desired graph state.</param>
    /// <param name="deleteMissingChildren">True deletes existing child rows missing from the supplied graph.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    public async Task<int> UpdateGraphAsync<T>(T entity, bool deleteMissingChildren = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var affected = await UpdateGraphNodeAsync(connection, transaction, entity!, deleteMissingChildren, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Loads a parent entity and selected child collections into an aggregate object.
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <param name="id">The aggregate root primary-key value.</param>
    /// <param name="includes">Collection property names to load. When null, all public collection properties are loaded.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The aggregate root, or null when the parent does not exist.</returns>
    public async Task<T?> GetGraphAsync<T>(object id, IEnumerable<string>? includes = null, CancellationToken cancellationToken = default)
    {
        var parent = await GetByIdAsync<T>(id, cancellationToken);
        if (parent is null) return default;

        var includeSet = includes is null
            ? null
            : new HashSet<string>(includes, StringComparer.OrdinalIgnoreCase);

        foreach (var collection in GetChildCollectionProperties(typeof(T)))
        {
            if (includeSet is not null && !includeSet.Contains(collection.Name)) continue;
            if (!collection.CanWrite && collection.GetValue(parent) is null) continue;

            var childType = GetCollectionItemType(collection.PropertyType);
            if (childType is null) continue;

            var fk = FindForeignKeyProperty(childType, typeof(T));
            if (fk is null) continue;

            var childShape = ForgeEntityShape.For(childType);
            var sql = $"SELECT * FROM {childShape.TableName} WHERE {ForgeEntityShape.ColumnName(fk)} = @ParentId";
            var rows = await QueryDynamicListAsync(childType, sql, new Dictionary<string, object?> { ["ParentId"] = id }, cancellationToken);
            AssignCollection(parent!, collection, childType, rows);
        }

        return parent;
    }

    /// <summary>
    /// Deletes child rows first and then deletes the parent row in one transaction.
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <param name="id">The aggregate root primary-key value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    public async Task<int> DeleteGraphAsync<T>(object id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var affected = 0;
            foreach (var collection in GetChildCollectionProperties(typeof(T)))
            {
                var childType = GetCollectionItemType(collection.PropertyType);
                if (childType is null) continue;

                var fk = FindForeignKeyProperty(childType, typeof(T));
                if (fk is null) continue;

                var childShape = ForgeEntityShape.For(childType);
                var sql = $"DELETE FROM {childShape.TableName} WHERE {ForgeEntityShape.ColumnName(fk)} = @ParentId";
                affected += await ForgeAdo.ExecuteAsync(connection, sql, new Dictionary<string, object?> { ["ParentId"] = id }, transaction, cancellationToken: cancellationToken);
            }

            var parentCommand = Provider.BuildDelete(_metadata.Resolve<T>(), id);
            affected += await ForgeAdo.ExecuteAsync(
                connection,
                parentCommand.CommandText,
                parentCommand.Parameters,
                transaction,
                parentCommand.CommandType,
                parentCommand.TimeoutSeconds,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task InsertGraphNodeAsync(DbConnection connection, DbTransaction transaction, object entity, object? parentKeyValue, Type? parentType, CancellationToken cancellationToken)
    {
        var entityType = entity.GetType();
        var shape = ForgeEntityShape.For(entityType);
        var key = shape.KeyProperty ?? throw new InvalidOperationException($"ForgeORM graph insert requires a key property on {entityType.Name}.");

        if (parentKeyValue is not null && parentType is not null)
        {
            var fk = FindForeignKeyProperty(entityType, parentType);
            if (fk is not null && fk.CanWrite)
                fk.SetValue(entity, ForgeObjectMapper.ConvertTo(parentKeyValue, fk.PropertyType));
        }

        // For SQL Server IDENTITY keys (int/long/short), never send an explicit Id value.
        // InsertSingleNodeAsync will use SCOPE_IDENTITY() and write the generated key back.
        ResetDatabaseGeneratedIdentity(entity, shape);

        await InsertSingleNodeAsync(connection, transaction, entity, shape, cancellationToken);
        var keyValue = key.GetValue(entity);

        foreach (var collection in GetChildCollectionProperties(entityType))
        {
            var children = ReadEnumerable(collection.GetValue(entity));
            foreach (var child in children)
                await InsertGraphNodeAsync(connection, transaction, child, keyValue, entityType, cancellationToken);
        }
    }

    private async Task<int> UpdateGraphNodeAsync(DbConnection connection, DbTransaction transaction, object entity, bool deleteMissingChildren, CancellationToken cancellationToken)
    {
        var entityType = entity.GetType();
        var shape = ForgeEntityShape.For(entityType);
        var key = shape.KeyProperty ?? throw new InvalidOperationException($"ForgeORM graph update requires a key property on {entityType.Name}.");
        var keyValue = key.GetValue(entity);
        var affected = await ExecuteSingleUpdateAsync(connection, transaction, entity, cancellationToken);

        foreach (var collection in GetChildCollectionProperties(entityType))
        {
            var childType = GetCollectionItemType(collection.PropertyType);
            if (childType is null) continue;

            var fk = FindForeignKeyProperty(childType, entityType);
            var childKey = ForgeEntityShape.For(childType).KeyProperty;
            var children = ReadEnumerable(collection.GetValue(entity)).ToList();

            if (deleteMissingChildren && fk is not null && keyValue is not null && childKey is not null)
            {
                var suppliedKeys = children.Select(x => childKey.GetValue(x)).Where(IsMeaningfulKey).ToList();
                var childShape = ForgeEntityShape.For(childType);
                var deleteSql = suppliedKeys.Count == 0
                    ? $"DELETE FROM {childShape.TableName} WHERE {ForgeEntityShape.ColumnName(fk)} = @ParentId"
                    : $"DELETE FROM {childShape.TableName} WHERE {ForgeEntityShape.ColumnName(fk)} = @ParentId AND {ForgeEntityShape.ColumnName(childKey)} NOT IN @Ids";
                var parameters = new Dictionary<string, object?> { ["ParentId"] = keyValue, ["Ids"] = suppliedKeys };
                affected += await ForgeAdo.ExecuteAsync(connection, deleteSql, parameters, transaction, cancellationToken: cancellationToken);
            }

            foreach (var child in children)
            {
                if (fk is not null && fk.CanWrite)
                    fk.SetValue(child, ForgeObjectMapper.ConvertTo(keyValue, fk.PropertyType));

                var childShape = ForgeEntityShape.For(child.GetType());
                var currentChildKey = childShape.KeyProperty?.GetValue(child);
                if (IsMeaningfulKey(currentChildKey))
                {
                    affected += await ExecuteSingleUpdateAsync(connection, transaction, child, cancellationToken);
                }
                else
                {
                    ResetDatabaseGeneratedIdentity(child, childShape);
                    affected += await InsertSingleNodeAsync(connection, transaction, child, childShape, cancellationToken);
                }
            }
        }

        return affected;
    }

    private async Task<int> InsertSingleNodeAsync(DbConnection connection, DbTransaction transaction, object entity, ForgeEntityShape shape, CancellationToken cancellationToken)
    {
        var key = shape.KeyProperty;
        if (key is not null)
        {
            EnsureKeyValue(entity, key);
            ResetDatabaseGeneratedIdentity(entity, shape);
        }

        var keyValue = key?.GetValue(entity);
        var includeKey = key is not null && ShouldIncludeGraphKeyInInsert(key, keyValue);
        var props = ForgeGraphWriteHelpers.GetInsertProperties(shape, includeKey);
        var sql = ForgeGraphWriteHelpers.BuildInsertSql(shape, props, includeScopeIdentity: key is not null && !includeKey);

        var parameters = ForgeGraphWriteHelpers.CreateParameterDictionary(props, entity);
        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction);

        if (key is not null && !includeKey)
        {
            var generated = await command.ExecuteScalarAsync(cancellationToken);
            if (key.CanWrite)
                key.SetValue(entity, ForgeObjectMapper.ConvertTo(generated, key.PropertyType));
            return 1;
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> ExecuteSingleUpdateAsync(DbConnection connection, DbTransaction transaction, object entity, CancellationToken cancellationToken)
    {
        var entityType = entity.GetType();
        var shape = ForgeEntityShape.For(entityType);
        var key = shape.KeyProperty ?? throw new InvalidOperationException($"ForgeORM graph update requires a key property on {entityType.Name}.");
        var props = ForgeGraphWriteHelpers.GetUpdateProperties(shape);
        var setClause = string.Join(", ", props.Select(p => $"{ForgeEntityShape.ColumnName(p)} = @{p.Name}"));
        var sql = $"UPDATE {shape.TableName} SET {setClause} WHERE {ForgeEntityShape.ColumnName(key)} = @{key.Name}";
        var parameters = ForgeGraphWriteHelpers.CreateParameterDictionary(props, entity);
        parameters[key.Name] = ForgeGraphWriteHelpers.NormalizeDatabaseValue(key.GetValue(entity), key);
        return await ForgeAdo.ExecuteAsync(connection, sql, parameters, transaction, cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<object>> QueryDynamicListAsync(Type type, string sql, object? parameters, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(MapRecord(type, reader));
        return rows;
    }

    private static object MapRecord(Type type, IDataRecord record)
    {
        var instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Cannot create {type.Name}.");
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && ForgeMaterializer.IsScalar(p.PropertyType))
            .ToList();
        for (var i = 0; i < record.FieldCount; i++)
        {
            var name = record.GetName(i);
            var prop = props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || ForgeEntityShape.ColumnName(p).Equals(name, StringComparison.OrdinalIgnoreCase));
            if (prop is null || record.IsDBNull(i)) continue;
            prop.SetValue(instance, ForgeObjectMapper.ConvertTo(record.GetValue(i), prop.PropertyType));
        }
        return instance;
    }

    private static IReadOnlyList<PropertyInfo> GetChildCollectionProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && GetCollectionItemType(p.PropertyType) is not null)
            .ToList();

    private static Type? GetCollectionItemType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        if (type.IsGenericType && type.GetGenericArguments().Length == 1) return type.GetGenericArguments()[0];
        return type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
    }

    private static IEnumerable<object> ReadEnumerable(object? value)
    {
        if (value is null) yield break;
        foreach (var item in (IEnumerable)value)
            if (item is not null) yield return item;
    }

    private static PropertyInfo? FindForeignKeyProperty(Type childType, Type parentType)
    {
        var parentName = parentType.Name;
        return childType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name.Equals(parentName + "Id", StringComparison.OrdinalIgnoreCase))
            ?? childType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && p.Name.Contains(parentName, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssignCollection(object parent, PropertyInfo property, Type childType, IReadOnlyList<object> rows)
    {
        if (property.CanWrite)
        {
            var listType = typeof(List<>).MakeGenericType(childType);
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var row in rows) list.Add(row);
            property.SetValue(parent, list);
            return;
        }

        if (property.GetValue(parent) is IList existing)
        {
            foreach (var row in rows) existing.Add(row);
        }
    }

    private static bool IsMeaningfulKey(object? value)
    {
        if (value is null) return false;
        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (type == typeof(Guid)) return (Guid)value != Guid.Empty;
        if (type.IsValueType) return !Equals(value, Activator.CreateInstance(type));
        return true;
    }

    private static void ResetDatabaseGeneratedIdentity(object entity, ForgeEntityShape shape)
    {
        var key = shape.KeyProperty;
        if (key is null || !key.CanWrite)
            return;

        var keyType = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;

        // GUID keys are client-generated. Numeric keys are database-generated identities.
        if (keyType == typeof(Guid))
            return;

        if (keyType == typeof(int) || keyType == typeof(long) || keyType == typeof(short))
            key.SetValue(entity, Activator.CreateInstance(keyType));
    }

    private static bool ShouldIncludeGraphKeyInInsert(PropertyInfo key, object? value)
    {
        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        if (type == typeof(Guid)) return true;
        if (value is null) return false;
        if (Equals(value, Activator.CreateInstance(type))) return false;
        return type != typeof(int) && type != typeof(long) && type != typeof(short);
    }
}
