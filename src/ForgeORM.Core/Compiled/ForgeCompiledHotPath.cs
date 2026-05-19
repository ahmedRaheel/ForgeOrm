using System.Collections.Concurrent;
using ForgeORM.Core.Performance;

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
        var runtime = ForgeRuntimeEntityMetadataCache.For(type);
        var props = runtime.Properties
            .Select(p => new ForgeCompiledPropertyAccessor
            {
                Name = p.PropertyName,
                PropertyType = p.PropertyType,
                Getter = p.Getter,
                Setter = p.Setter
            })
            .ToList();

        var key = runtime.Key is null
            ? null
            : props.FirstOrDefault(x => x.Name.Equals(runtime.Key.PropertyName, StringComparison.OrdinalIgnoreCase));

        return new ForgeCompiledEntityPlan
        {
            EntityType = type,
            TableName = runtime.TableName,
            Properties = props,
            Key = key,
            InsertSql = runtime.InsertSql,
            SelectByIdSql = runtime.Key is null
                ? $"SELECT {runtime.SelectColumnsSql} FROM {runtime.TableName};"
                : $"SELECT {runtime.SelectColumnsSql} FROM {runtime.TableName} WHERE {runtime.Key.ColumnName} = @{runtime.Key.PropertyName};",
            UpdateSql = runtime.UpdateSql,
            DeleteSql = runtime.DeleteSql
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
