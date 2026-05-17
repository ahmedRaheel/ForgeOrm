namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Represents an index suggestion.
/// </summary>
public sealed record ForgeIndexSuggestion(string Name, string Sql, string Reason);
