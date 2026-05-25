namespace ForgeORM.Intelligence;

public sealed class ForgeSqlSuggestionResult
{
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Suggestions { get; init; } = [];
}
