namespace ForgeORM.Core.SavedQueries;

/// <summary>
/// Represents a reusable SQL query definition.
/// </summary>
public sealed class ForgeSavedQuery
{
    public required string Name { get; init; }

    public required string Sql { get; init; }

    public object? Parameters { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
