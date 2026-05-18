using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core.Compiled;

/// <summary>
/// Compiled property accessor to avoid repeated reflection in hot paths.
/// </summary>
public sealed class ForgeCompiledPropertyAccessor
{
    public required string Name { get; init; }
    public required Type PropertyType { get; init; }
    public required Func<object, object?> Getter { get; init; }
    public required Action<object, object?>? Setter { get; init; }
}

/// <summary>
/// Compiled entity shape containing getters/setters and generated SQL fragments.
/// </summary>
public sealed class ForgeCompiledEntityPlan
{
    public required Type EntityType { get; init; }
    public required string TableName { get; init; }
    public required IReadOnlyList<ForgeCompiledPropertyAccessor> Properties { get; init; }
    public required ForgeCompiledPropertyAccessor? Key { get; init; }
    public required string InsertSql { get; init; }
    public required string SelectByIdSql { get; init; }
    public required string UpdateSql { get; init; }
    public required string DeleteSql { get; init; }
}

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
        var instance = Expression.Parameter(typeof(object), "instance");
        var typed = Expression.Convert(instance, property.DeclaringType!);
        var access = Expression.Property(typed, property);
        var box = Expression.Convert(access, typeof(object));
        var getter = Expression.Lambda<Func<object, object?>>(box, instance).Compile();

        Action<object, object?>? setter = null;

        if (property.CanWrite)
        {
            var value = Expression.Parameter(typeof(object), "value");
            var converted = Expression.Convert(value, property.PropertyType);
            var assign = Expression.Assign(access, converted);
            setter = Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
        }

        return new ForgeCompiledPropertyAccessor
        {
            Name = property.Name,
            PropertyType = property.PropertyType,
            Getter = getter,
            Setter = setter
        };
    }
}

/// <summary>
/// Compiled graph plan placeholder for source generator output.
/// </summary>
public sealed record ForgeCompiledGraphPlan(
    Type ParentType,
    IReadOnlyList<Type> ChildTypes,
    string Strategy,
    DateTimeOffset CompiledAtUtc);

public static class ForgeCompiledGraphPlanCache
{
    private static readonly ConcurrentDictionary<Type, ForgeCompiledGraphPlan> Cache = new();

    public static ForgeCompiledGraphPlan For<TParent>(params Type[] childTypes)
        => Cache.GetOrAdd(typeof(TParent), t => new ForgeCompiledGraphPlan(t, childTypes, "CompiledGraphPlan", DateTimeOffset.UtcNow));
}
