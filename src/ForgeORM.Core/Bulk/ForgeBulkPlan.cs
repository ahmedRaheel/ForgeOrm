
namespace ForgeORM.Core.Bulk;

public sealed class ForgeBulkPlan
{
    public required string TableName { get; init; }
    public required string QuotedTableName { get; init; }
    public required string InsertSql { get; init; }
    public required string UpdateSql { get; init; }
    public required string DeleteSql { get; init; }
    public required string KeyColumn { get; init; }

    public string? TvpTypeName { get; init; }
    public string? KeyTvpTypeName { get; init; }
    public string? TempTableName { get; init; }
}
