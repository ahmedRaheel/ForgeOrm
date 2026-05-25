namespace ForgeORM.Abstractions;

public sealed record ForgeReportRequest(
    string Name,
    string From,
    IReadOnlyList<ForgeReportColumn> Columns,
    IReadOnlyList<ForgeReportFilter> Filters,
    string? GroupBy = null,
    string? OrderBy = null,
    int? Skip = null,
    int? Take = null);
