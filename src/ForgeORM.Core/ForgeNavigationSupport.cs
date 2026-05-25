using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal static class ForgeNavigationSupport
{
    public static async ValueTask LoadIncludedNavigationsAsync<T>(
        IReadOnlyList<T> rows,
        IForgeDb db,
        string keyColumn,
        IReadOnlyList<PropertyInfo> includes,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0 || includes.Count == 0)
            return;

        foreach (var navigation in includes)
        {
            if (!navigation.CanWrite)
                continue;

            if (IsCollectionNavigation(navigation))
            {
                await LoadCollectionNavigationAsync(rows, db, keyColumn, navigation, cancellationToken);
                continue;
            }

            if (IsReferenceNavigation(navigation))
                await LoadReferenceNavigationAsync(rows, db, navigation, cancellationToken);
        }
    }

    public static bool IsCollectionNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
            return false;

        return property.PropertyType.IsGenericType
            && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
    }

    public static bool IsReferenceNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
            return false;

        if (IsCollectionNavigation(property))
            return false;

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type.IsClass && !IsScalarColumnType(type);
    }

    private static async ValueTask LoadCollectionNavigationAsync<T>(
        IReadOnlyList<T> parents,
        IForgeDb db,
        string keyColumn,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        if (parents.Count == 0)
            return;

        var childType = navigation.PropertyType.GetGenericArguments()[0];
        var parentKey = typeof(T).GetProperty(keyColumn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (parentKey is null)
            return;

        var childForeignKeyName = typeof(T).Name + "Id";
        var childForeignKey = childType.GetProperty(childForeignKeyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (childForeignKey is null)
            return;

        var ids = parents
            .Select(parent => ForgeRuntimeAccessorCache.Get(parentKey, parent!))
            .Where(x => x is not null)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return;

        var childTable = ResolveTableName(childType);
        var childColumns = ResolveScalarColumns(childType);
        var sql = $"SELECT {childColumns} FROM {childTable} WHERE {childForeignKey.Name} IN @Ids";

        var queryAsync = typeof(IForgeDb).GetMethods()
            .Where(x => x.Name == nameof(IForgeDb.QueryAsync) && x.IsGenericMethodDefinition)
            .First(x => x.GetParameters().Length >= 2)
            .MakeGenericMethod(childType);

        var awaitable = queryAsync.Invoke(db, new object?[] { sql, new { Ids = ids }, null, cancellationToken })!;
        var result = await ForgeRuntimeMemberCache.AwaitAndGetResultAsync(awaitable).ConfigureAwait(false)
            as System.Collections.IEnumerable;
        if (result is null)
            return;

        var children = result.Cast<object>().ToList();
        foreach (var parent in parents)
        {
            var parentId = ForgeRuntimeAccessorCache.Get(parentKey, parent!);
            var list = (System.Collections.IList)ForgeRuntimeAccessorCache.Constructor(typeof(List<>).MakeGenericType(childType))();

            foreach (var child in children)
            {
                var fk = ForgeRuntimeAccessorCache.Get(childForeignKey, child);
                if (Equals(fk, parentId))
                    list.Add(child);
            }

            ForgeRuntimeAccessorCache.Set(navigation, parent!, list);
        }
    }

    private static async ValueTask LoadReferenceNavigationAsync<T>(
        IReadOnlyList<T> parents,
        IForgeDb db,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        var childType = navigation.PropertyType;
        var fkProperty = typeof(T).GetProperty(navigation.Name + "Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (fkProperty is null)
            return;

        var childTable = ResolveTableName(childType);
        var childColumns = ResolveScalarColumns(childType);

        var queryFirstOrDefaultAsync = typeof(IForgeDb).GetMethods()
            .Where(x => x.Name == nameof(IForgeDb.QueryFirstOrDefaultAsync) && x.IsGenericMethodDefinition)
            .First(x => x.GetParameters().Length >= 2)
            .MakeGenericMethod(childType);

        foreach (var parent in parents)
        {
            var fkValue = ForgeRuntimeAccessorCache.Get(fkProperty, parent!);
            if (fkValue is null)
                continue;

            var sql = $"SELECT {childColumns} FROM {childTable} WHERE Id = @Id";
            var awaitable = queryFirstOrDefaultAsync.Invoke(db, new object?[] { sql, new { Id = fkValue }, null, cancellationToken })!;
            var child = await ForgeRuntimeMemberCache.AwaitAndGetResultAsync(awaitable).ConfigureAwait(false);
            ForgeRuntimeAccessorCache.Set(navigation, parent!, child);
        }
    }

    private static string ResolveTableName(Type type)
        => type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name;

    private static string ResolveScalarColumns(Type type)
    {
        var columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => IsScalarColumnType(x.PropertyType))
            .Select(x => x.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return columns.Length == 0 ? "*" : string.Join(", ", columns);
    }

    private static bool IsScalarColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(byte[]);
    }
}
