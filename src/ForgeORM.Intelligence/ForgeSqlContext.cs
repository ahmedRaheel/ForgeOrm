namespace ForgeORM.Intelligence;

public sealed class ForgeSqlContext
{
    public string ProviderName { get; init; } = "SqlServer";
    public IReadOnlyList<ForgeTableSchema> Tables { get; init; } = [];
}
