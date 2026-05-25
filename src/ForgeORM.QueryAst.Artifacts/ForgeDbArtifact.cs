using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public sealed class ForgeDbArtifact
{
    public required ForgeDbArtifactType Type { get; init; }
    public required string Schema { get; init; }
    public required string Name { get; init; }
    public required string SqlDefinition { get; init; }
    public string? ChangeReason { get; init; }
    public string FullName => string.IsNullOrWhiteSpace(Schema) ? Name : $"{Schema}.{Name}";
}
