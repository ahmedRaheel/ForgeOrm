using System.Collections.Concurrent;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.Core.Compiled;

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
