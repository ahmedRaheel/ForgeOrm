namespace ForgeORM.Intelligence;

public sealed class ForgeSqlCompletionItem
{
    public required string Label { get; init; }
    public required string InsertText { get; init; }
    public string Kind { get; init; } = "Keyword";
    public string? Description { get; init; }
}
