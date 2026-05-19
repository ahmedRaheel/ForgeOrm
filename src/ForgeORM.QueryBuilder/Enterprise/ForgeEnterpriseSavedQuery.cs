namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Represents a named reusable query.
/// </summary>
public sealed class ForgeEnterpriseSavedQuery
{
    public required string Name { get; init; }
    public required Func<IReadOnlyDictionary<string, object?>, ForgeSqlQuery> Factory { get; init; }
}
