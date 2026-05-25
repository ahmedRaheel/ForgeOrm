namespace ForgeORM.Intelligence;

public sealed class ForgeSqlCompletionResult
{
    public IReadOnlyList<ForgeSqlCompletionItem> Items { get; init; } = [];
}
