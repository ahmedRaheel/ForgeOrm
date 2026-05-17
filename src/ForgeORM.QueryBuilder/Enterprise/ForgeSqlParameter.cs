namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Represents a generated SQL parameter.
/// </summary>
public sealed record ForgeSqlParameter(string Name, object? Value);
