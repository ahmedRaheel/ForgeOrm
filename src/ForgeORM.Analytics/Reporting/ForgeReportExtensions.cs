using ForgeORM.Core;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// ForgeORM reporting extensions.
/// </summary>
public static class ForgeReportExtensions
{
    public static ForgeReportBuilder<T> Report<T>(this ForgeDb db, string? name = null)
        => new(db, name);

    public static ForgeReportBuilder<T> Report<T>(this ForgeDbContext db, string? name = null)
        => new(db, name);
}
