namespace ForgeORM.DataFrame.Enterprise;

/// <summary>
/// Defines a reusable report over a DataFrame.
/// </summary>
public sealed class ForgeReportDefinition
{
    public required string Name { get; init; }
    public required string RowDimension { get; init; }
    public string? ColumnDimension { get; init; }
    public required ForgeFrameMeasure Measure { get; init; }
}
