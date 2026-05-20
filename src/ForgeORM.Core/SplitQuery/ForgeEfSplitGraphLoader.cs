using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;

namespace ForgeORM.Core;

/// <summary>
/// EF-style split graph loader used by db.Set&lt;T&gt;().Include(...).AsSplitQuery().ToListAsync().
/// It does not require mapping attributes: table/column/key discovery is convention-first, then attribute-aware.
/// </summary>
internal static class ForgeEfSplitGraphLoader
{
    private static readonly ConcurrentDictionary<Type, ForgeEfEntityShape> Shapes = new();
    private static readonly MethodInfo QueryAsyncMethod = typeof(IForgeRawSql)
        .GetMethods()
        .First(x => x.Name == nameof(IForgeRawSql.QueryAsync) && x.IsGenericMethodDefinition);

    public static async Task LoadIncludedNavigationsAsync<T>(
        IReadOnlyList<T> rows,
        IForgeDb db,
        IReadOnlyList<PropertyInfo> includes,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0 || includes.Count == 0)
            return;

        var parentShape = GetShape(typeof(T));

        foreach (var navigation in includes)
        {
            if (!navigation.CanWrite)
                continue;

            if (IsCollectionNavigation(navigation))
            {
                await LoadCollectionNavigationAsync(rows.Cast<object>().ToList(), db, parentShape, navigation, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (IsReferenceNavigation(navigation))
            {
                await LoadReferenceNavigationAsync(rows.Cast<object>().ToList(), db, parentShape, navigation, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static async Task LoadCollectionNavigationAsync(
        IReadOnlyList<object> parents,
        IForgeDb db,
        ForgeEfEntityShape parentShape,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        var childType = navigation.PropertyType.GetGenericArguments()[0];
        var childShape = GetShape(childType);
        var childFk = ResolveChildForeignKey(childShape, parentShape);

        if (childFk is not null)
        {
            await LoadOneToManyAsync(parents, db, parentShape, childShape, childFk, navigation, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await LoadManyToManyByConventionAsync(parents, db, parentShape, childShape, navigation, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task LoadOneToManyAsync(
        IReadOnlyList<object> parents,
        IForgeDb db,
        ForgeEfEntityShape parentShape,
        ForgeEfEntityShape childShape,
        PropertyInfo childForeignKey,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        if (parentShape.KeyProperties.Count == 0)
            return;

        if (parentShape.KeyProperties.Count == 1)
        {
            var parentKey = parentShape.KeyProperties[0];
            var ids = parents.Select(x => ForgeRuntimeAccessorCache.Get(parentKey, x)).Where(x => x is not null).Distinct().ToArray();
            if (ids.Length == 0) return;

            var filter = ForgeSplitQueryBatching.BuildIdFilter(childForeignKey.Name, "Ids", ids);
            var sql = $"SELECT {childShape.ColumnList} FROM {childShape.TableName} WHERE {filter.Predicate}";
            var children = await QueryObjectsAsync(db, childShape.EntityType, sql, filter.Parameters, cancellationToken)
                .ConfigureAwait(false);

            var lookup = children.ToLookup(x => ForgeRuntimeAccessorCache.Get(childForeignKey, x));
            foreach (var parent in parents)
            {
                var key = ForgeRuntimeAccessorCache.Get(parentKey, parent);
                AssignCollection(parent, navigation, childShape.EntityType, lookup[key].ToList());
            }

            return;
        }

        var where = BuildCompositeWhere(parentShape.KeyProperties, childShape, parents, "p", out var parameters, childForeignKeyOverride: childForeignKey);
        if (where.Length == 0) return;

        var compositeSql = $"SELECT {childShape.ColumnList} FROM {childShape.TableName} WHERE {where}";
        var compositeChildren = await QueryObjectsAsync(db, childShape.EntityType, compositeSql, parameters, cancellationToken).ConfigureAwait(false);
        foreach (var parent in parents)
        {
            var parentKey = CompositeKey(parentShape.KeyProperties, parent);
            var matches = compositeChildren.Where(child => CompositeChildForeignKey(parentShape, childShape, child, childForeignKey) == parentKey).ToList();
            AssignCollection(parent, navigation, childShape.EntityType, matches);
        }
    }

    private static async Task LoadReferenceNavigationAsync(
        IReadOnlyList<object> parents,
        IForgeDb db,
        ForgeEfEntityShape parentShape,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        var referenceType = Nullable.GetUnderlyingType(navigation.PropertyType) ?? navigation.PropertyType;
        var referenceShape = GetShape(referenceType);

        if (referenceShape.KeyProperties.Count == 0)
            return;

        var referenceKey = referenceShape.KeyProperties[0];
        var parentFk = FindProperty(parentShape.EntityType, referenceType.Name + referenceKey.Name)
            ?? FindProperty(parentShape.EntityType, referenceType.Name + "Id")
            ?? FindProperty(parentShape.EntityType, referenceKey.Name);

        if (parentFk is null)
            return;

        var ids = parents.Select(x => ForgeRuntimeAccessorCache.Get(parentFk, x)).Where(x => x is not null).Distinct().ToArray();
        if (ids.Length == 0) return;

        var filter = ForgeSplitQueryBatching.BuildIdFilter(referenceKey.Name, "Ids", ids);
        var sql = $"SELECT {referenceShape.ColumnList} FROM {referenceShape.TableName} WHERE {filter.Predicate}";
        var references = await QueryObjectsAsync(db, referenceShape.EntityType, sql, filter.Parameters, cancellationToken)
            .ConfigureAwait(false);

        var map = references
            .GroupBy(x => ForgeRuntimeAccessorCache.Get(referenceKey, x))
            .ToDictionary(x => x.Key, x => x.First(), EqualityComparer<object?>.Default);

        foreach (var parent in parents)
        {
            var fk = ForgeRuntimeAccessorCache.Get(parentFk, parent);
            if (fk is not null && map.TryGetValue(fk, out var related))
                ForgeRuntimeAccessorCache.Set(navigation, parent, related);
        }
    }

    private static async Task LoadManyToManyByConventionAsync(
        IReadOnlyList<object> parents,
        IForgeDb db,
        ForgeEfEntityShape parentShape,
        ForgeEfEntityShape childShape,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        if (parentShape.KeyProperties.Count != 1 || childShape.KeyProperties.Count != 1)
            return;

        var parentKey = parentShape.KeyProperties[0];
        var childKey = childShape.KeyProperties[0];
        var parentIds = parents.Select(x => ForgeRuntimeAccessorCache.Get(parentKey, x)).Where(x => x is not null).Distinct().ToArray();
        if (parentIds.Length == 0) return;

        var joinTable = ResolveJoinTableName(parentShape, childShape);
        var joinParentColumn = parentKey.Name;
        var joinChildColumn = childKey.Name;

        var joinFilter = ForgeSplitQueryBatching.BuildIdFilter(joinParentColumn, "Ids", parentIds);
        var joinSql = $"SELECT {joinParentColumn}, {joinChildColumn} FROM {joinTable} WHERE {joinFilter.Predicate}";
        var joinRows = await QueryDictionaryRowsAsync(db, joinSql, joinFilter.Parameters, cancellationToken)
            .ConfigureAwait(false);

        var childIds = joinRows.Select(x => GetDictionaryValue(x, joinChildColumn)).Where(x => x is not null).Distinct().ToArray();
        if (childIds.Length == 0)
        {
            foreach (var parent in parents)
                AssignCollection(parent, navigation, childShape.EntityType, []);
            return;
        }

        var childFilter = ForgeSplitQueryBatching.BuildIdFilter(childKey.Name, "Ids", childIds);
        var childSql = $"SELECT {childShape.ColumnList} FROM {childShape.TableName} WHERE {childFilter.Predicate}";
        var children = await QueryObjectsAsync(db, childShape.EntityType, childSql, childFilter.Parameters, cancellationToken)
            .ConfigureAwait(false);

        var childMap = children
            .GroupBy(x => ForgeRuntimeAccessorCache.Get(childKey, x))
            .ToDictionary(x => x.Key, x => x.First(), EqualityComparer<object?>.Default);

        var joinsByParent = joinRows.ToLookup(x => GetDictionaryValue(x, joinParentColumn));
        foreach (var parent in parents)
        {
            var parentId = ForgeRuntimeAccessorCache.Get(parentKey, parent);
            var related = joinsByParent[parentId]
                .Select(x => GetDictionaryValue(x, joinChildColumn))
                .Where(x => x is not null && childMap.ContainsKey(x))
                .Select(x => childMap[x])
                .ToList();

            AssignCollection(parent, navigation, childShape.EntityType, related);
        }
    }

    private static string ResolveJoinTableName(ForgeEfEntityShape parentShape, ForgeEfEntityShape childShape)
    {
        // Convention-first: OrderProduct, ProductOrder. Users can still use explicit SplitGraph when table names differ.
        return parentShape.EntityType.Name + childShape.EntityType.Name;
    }

    private static async Task<IReadOnlyList<object>> QueryObjectsAsync(
        IForgeDb db,
        Type type,
        string sql,
        object? parameters,
        CancellationToken cancellationToken)
    {
        var method = QueryAsyncMethod.MakeGenericMethod(type);
        var task = (Task)method.Invoke(db, [sql, parameters, null, cancellationToken])!;
        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task) as IEnumerable;
        return result?.Cast<object>().ToList() ?? [];
    }

    private static async Task<IReadOnlyList<Dictionary<string, object?>>> QueryDictionaryRowsAsync(
        IForgeDb db,
        string sql,
        object? parameters,
        CancellationToken cancellationToken)
    {
        if (db is ForgeDb forgeDb)
            return await forgeDb.QueryDictionaryAsync(sql, parameters, cancellationToken: cancellationToken).ConfigureAwait(false);

        var method = db.GetType().GetMethod("QueryDictionaryAsync", [typeof(string), typeof(object), typeof(int?), typeof(CancellationToken)]);
        if (method is null)
            throw new NotSupportedException("Many-to-many convention loading requires QueryDictionaryAsync on ForgeDb.");

        var task = (Task)method.Invoke(db, [sql, parameters, null, cancellationToken])!;
        await task.ConfigureAwait(false);
        return (IReadOnlyList<Dictionary<string, object?>>)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static void AssignCollection(object parent, PropertyInfo navigation, Type childType, IReadOnlyList<object> values)
    {
        var listType = typeof(List<>).MakeGenericType(childType);
        var list = (IList)ForgeRuntimeAccessorCache.Constructor(listType)();
        foreach (var value in values)
            list.Add(value);
        ForgeRuntimeAccessorCache.Set(navigation, parent, list);
    }

    private static PropertyInfo? ResolveChildForeignKey(ForgeEfEntityShape childShape, ForgeEfEntityShape parentShape)
    {
        var parentKey = parentShape.KeyProperties.FirstOrDefault();
        var candidates = new List<string>();
        if (parentKey is not null)
        {
            candidates.Add(parentShape.EntityType.Name + parentKey.Name);
            candidates.Add(parentShape.EntityType.Name + "Id");
            candidates.Add(parentKey.Name);
        }

        return candidates
            .Select(name => FindProperty(childShape.EntityType, name))
            .FirstOrDefault(property => property is not null);
    }

    private static string BuildCompositeWhere(
        IReadOnlyList<PropertyInfo> parentKeys,
        ForgeEfEntityShape childShape,
        IReadOnlyList<object> parents,
        string prefix,
        out Dictionary<string, object?> parameters,
        PropertyInfo? childForeignKeyOverride = null)
    {
        parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parentKeys.Count == 0 || parents.Count == 0)
            return string.Empty;

        var clauses = new List<string>();
        for (var row = 0; row < parents.Count; row++)
        {
            var childColumns = parentKeys
                .Select(parentKey => childForeignKeyOverride ?? FindProperty(childShape.EntityType, parentKey.Name) ?? FindProperty(childShape.EntityType, parentKey.DeclaringType!.Name + parentKey.Name))
                .ToArray();

            if (childColumns.Any(x => x is null))
                continue;

            var parts = new List<string>();
            for (var col = 0; col < childColumns.Length; col++)
            {
                var paramName = $"{prefix}_{row}_{col}";
                parameters[paramName] = ForgeRuntimeAccessorCache.Get(parentKeys[col], parents[row]);
                parts.Add($"{childColumns[col]!.Name} = @{paramName}");
            }

            clauses.Add("(" + string.Join(" AND ", parts) + ")");
        }

        return string.Join(" OR ", clauses);
    }

    private static string CompositeKey(IReadOnlyList<PropertyInfo> properties, object instance)
        => string.Join("|", properties.Select(x => ForgeRuntimeAccessorCache.Get(x, instance)?.ToString() ?? string.Empty));

    private static string CompositeChildForeignKey(ForgeEfEntityShape parentShape, ForgeEfEntityShape childShape, object child, PropertyInfo childForeignKey)
    {
        if (parentShape.KeyProperties.Count == 1)
            return ForgeRuntimeAccessorCache.Get(childForeignKey, child)?.ToString() ?? string.Empty;

        var values = parentShape.KeyProperties.Select(parentKey =>
        {
            var property = FindProperty(childShape.EntityType, parentKey.Name)
                ?? FindProperty(childShape.EntityType, parentShape.EntityType.Name + parentKey.Name)
                ?? childForeignKey;
            return ForgeRuntimeAccessorCache.Get(property, child)?.ToString() ?? string.Empty;
        });

        return string.Join("|", values);
    }

    private static object? GetDictionaryValue(Dictionary<string, object?> row, string name)
    {
        if (row.TryGetValue(name, out var value)) return value;
        var normalized = NormalizeName(name);
        foreach (var item in row)
        {
            if (NormalizeName(item.Key) == normalized)
                return item.Value;
        }
        return null;
    }

    private static ForgeEfEntityShape GetShape(Type type) => Shapes.GetOrAdd(type, CreateShape);

    private static ForgeEfEntityShape CreateShape(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.GetIndexParameters().Length == 0)
            .ToArray();

        var graph = ForgeEntityMetadataCache.Get(type);
        var keyProperties = ResolveKeyProperties(type, properties, graph.KeyProperty).ToArray();
        var scalarProperties = properties.Where(IsScalarColumn).ToArray();

        return new ForgeEfEntityShape(
            type,
            graph.TableName,
            keyProperties,
            scalarProperties,
            scalarProperties.Length == 0 ? "*" : string.Join(", ", scalarProperties.Select(x => x.Name)));
    }

    private static IEnumerable<PropertyInfo> ResolveKeyProperties(Type type, IReadOnlyList<PropertyInfo> properties, PropertyInfo? graphKey)
    {
        var explicitKeys = properties
            .Select(property => new { property, order = GetKeyOrder(property) })
            .Where(x => x.order.HasValue)
            .OrderBy(x => x.order!.Value)
            .Select(x => x.property)
            .ToArray();

        if (explicitKeys.Length > 0)
            return explicitKeys;

        if (graphKey is not null)
            return [graphKey];

        var id = FindProperty(type, "Id");
        if (id is not null)
            return [id];

        var entityId = FindProperty(type, type.Name + "Id");
        if (entityId is not null)
            return [entityId];

        var anyId = properties.FirstOrDefault(x => x.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
        return anyId is null ? [] : [anyId];
    }

    private static int? GetKeyOrder(PropertyInfo property)
    {
        foreach (var attribute in property.GetCustomAttributes(false))
        {
            var name = attribute.GetType().Name;
            if (name is not "ForgeKeyAttribute" and not "KeyAttribute")
                continue;

            var orderProperty = attribute.GetType().GetProperty("Order");
            if (orderProperty?.GetValue(attribute) is int order)
                return order;

            return 0;
        }

        return null;
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        var normalized = NormalizeName(name);
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(x => NormalizeName(x.Name) == normalized);
    }

    private static bool IsCollectionNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string)) return false;
        return property.PropertyType.IsGenericType
            && typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
            && property.PropertyType.GetGenericArguments().Length == 1
            && !IsScalarType(property.PropertyType.GetGenericArguments()[0]);
    }

    private static bool IsReferenceNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string)) return false;
        if (IsCollectionNavigation(property)) return false;
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type.IsClass && !IsScalarType(type);
    }

    private static bool IsScalarColumn(PropertyInfo property)
        => IsScalarType(Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);

    private static bool IsScalarType(Type type)
        => type.IsPrimitive
           || type.IsEnum
           || type == typeof(string)
           || type == typeof(decimal)
           || type == typeof(DateTime)
           || type == typeof(DateTimeOffset)
           || type == typeof(TimeSpan)
           || type == typeof(Guid)
           || type == typeof(byte[]);

    private static string NormalizeName(string name)
        => new(name.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}

internal sealed record ForgeEfEntityShape(
    Type EntityType,
    string TableName,
    IReadOnlyList<PropertyInfo> KeyProperties,
    IReadOnlyList<PropertyInfo> ScalarProperties,
    string ColumnList);
