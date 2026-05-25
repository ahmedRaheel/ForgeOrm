using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Cached scalar property metadata for one entity type. Reflection is used only when this plan is built.
/// </summary>
public sealed class ForgeRuntimeEntityPlan
{
    public required Type EntityType { get; init; }
    public required string TableName { get; init; }
    public required IReadOnlyList<ForgeRuntimePropertyPlan> Properties { get; init; }
    public required ForgeRuntimePropertyPlan? Key { get; init; }
    public required string SelectColumnsSql { get; init; }
    public required string InsertSql { get; init; }
    public required string UpdateSql { get; init; }
    public required string DeleteSql { get; init; }
}
