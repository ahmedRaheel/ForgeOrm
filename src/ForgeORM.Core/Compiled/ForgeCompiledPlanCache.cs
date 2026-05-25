using System.Collections.Concurrent;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.Core.Compiled;

/// <summary>
/// Compiles and caches entity plans. This is source-generator-compatible and removes reflection from hot paths after first access.
/// </summary>
public static class ForgeCompiledPlanCache
{
    private static readonly ConcurrentDictionary<Type, ForgeCompiledEntityPlan> Cache = new();

    public static ForgeCompiledEntityPlan For<TEntity>()
        => For(typeof(TEntity));

    public static ForgeCompiledEntityPlan For(Type entityType)
        => Cache.GetOrAdd(entityType, Build);

    private static ForgeCompiledEntityPlan Build(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .Select(CreateAccessor)
            .ToList();

        var key = props.FirstOrDefault(x => x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        var table = type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s";

        var nonKey = props.Where(p => key is null || !p.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        var columns = string.Join(", ", nonKey.Select(p => p.Name));
        var values = string.Join(", ", nonKey.Select(p => "@" + p.Name));
        var set = string.Join(", ", nonKey.Select(p => $"{p.Name} = @{p.Name}"));

        return new ForgeCompiledEntityPlan
        {
            EntityType = type,
            TableName = table,
            Properties = props,
            Key = key,
            InsertSql = $"INSERT INTO {table} ({columns}) VALUES ({values});",
            SelectByIdSql = key is null ? $"SELECT {BuildSelectColumns(props)} FROM {table};" : $"SELECT {BuildSelectColumns(props)} FROM {table} WHERE {key.Name} = @Id;",
            UpdateSql = key is null ? $"-- No key for {type.Name}" : $"UPDATE {table} SET {set} WHERE {key.Name} = @{key.Name};",
            DeleteSql = key is null ? $"-- No key for {type.Name}" : $"DELETE FROM {table} WHERE {key.Name} = @Id;"
        };
    }

    private static string BuildSelectColumns(IReadOnlyList<ForgeCompiledPropertyAccessor> props)
        => props.Count == 0 ? "*" : string.Join(", ", props.Select(p => p.Name));

    private static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive
               || actual.IsEnum
               || actual == typeof(string)
               || actual == typeof(Guid)
               || actual == typeof(decimal)
               || actual == typeof(DateTime)
               || actual == typeof(DateTimeOffset)
               || actual == typeof(DateOnly)
               || actual == typeof(TimeOnly)
               || actual == typeof(TimeSpan)
               || actual == typeof(byte[]);
    }

    private static ForgeCompiledPropertyAccessor CreateAccessor(PropertyInfo property)
    {
        var getter = ForgeRuntimeAccessorCache.Getter(property);
        Action<object, object?>? setter = property.CanWrite
            ? ForgeRuntimeAccessorCache.Setter(property)
            : null;

        return new ForgeCompiledPropertyAccessor
        {
            Name = property.Name,
            PropertyType = property.PropertyType,
            Getter = getter,
            Setter = setter
        };
    }
}
