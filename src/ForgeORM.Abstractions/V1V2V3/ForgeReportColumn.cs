namespace ForgeORM.Abstractions;

public sealed record ForgeReportColumn(string Name, string Expression, string? Alias = null);
