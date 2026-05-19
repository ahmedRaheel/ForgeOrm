namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Represents generated SQL and parameters.
/// </summary>
public sealed class ForgeSqlQuery
{
    public required string Sql { get; init; }
    public IReadOnlyList<ForgeSqlParameter> Parameters { get; init; } = Array.Empty<ForgeSqlParameter>();
}
