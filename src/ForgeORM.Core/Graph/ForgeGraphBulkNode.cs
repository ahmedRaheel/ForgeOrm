using ForgeORM.Core.Enums;

namespace ForgeORM.Core;

public sealed record ForgeGraphBulkNode(
    Type EntityType,
    string TableName,
    string? ParentKeyColumn,
    string? ForeignKeyColumn,
    int EstimatedRows);
