namespace ForgeORM.Intelligence;

public sealed class ForgeSqlCorrectionResult
{
    public bool Changed { get; init; }
    public required string Sql { get; init; }
    public IReadOnlyList<string> Fixes { get; init; } = [];
}
