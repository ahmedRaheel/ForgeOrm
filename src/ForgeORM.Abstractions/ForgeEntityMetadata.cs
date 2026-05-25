using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgeEntityMetadata
{
    public required Type EntityType { get; init; }
    public required string TableName { get; init; }
    public required string KeyColumn { get; init; }
    public string CodeColumn { get; init; } = "Code";
    public IReadOnlyList<ForgePropertyMetadata> Properties { get; init; } = [];
}
