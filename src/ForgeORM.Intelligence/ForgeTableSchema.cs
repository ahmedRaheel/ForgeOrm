namespace ForgeORM.Intelligence;

public sealed class ForgeTableSchema
{
    public required string Name { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = [];
}
