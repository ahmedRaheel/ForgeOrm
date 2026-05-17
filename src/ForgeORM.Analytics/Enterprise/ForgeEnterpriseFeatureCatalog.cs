namespace ForgeORM.Analytics.Enterprise;

/// <summary>
/// Documents enterprise ForgeORM feature modules enabled by this update.
/// </summary>
public static class ForgeEnterpriseFeatureCatalog
{
    public static IReadOnlyList<string> Features { get; } =
    [
        "Split query parent-child loading",
        "Dynamic search with WhereIf, Between, Contains, sorting and paging",
        "Expression-to-SQL generation",
        "Enterprise DataFrame pivot/group/rank/rolling analytics",
        "Report and pivot builder",
        "Saved query registry",
        "Query profiling",
        "Auto index suggestions",
        "In-memory query cache foundation",
        "Smart projection via selected expression columns"
    ];
}
