using ForgeORM.Core.Enums;

namespace ForgeORM.Core;

/// <summary>
/// Describes the provider bulk-first graph write strategy selected for graph insert/update/delete.
/// SQL Server should prefer TVP/temp-table MERGE/SqlBulkCopy for child collections instead of row-by-row writes.
/// </summary>
public sealed record ForgeGraphBulkWritePlan(
    Type RootType,
    string Provider,
    ForgeBulkStrategy Strategy,
    IReadOnlyList<ForgeGraphBulkNode> Nodes);

public sealed record ForgeGraphBulkNode(
    Type EntityType,
    string TableName,
    string? ParentKeyColumn,
    string? ForeignKeyColumn,
    int EstimatedRows);
