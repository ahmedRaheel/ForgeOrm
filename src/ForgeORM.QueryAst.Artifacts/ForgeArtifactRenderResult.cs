using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public sealed class ForgeArtifactRenderResult
{
    public required ForgeDbArtifact Artifact { get; init; }
    public required string DeploymentSql { get; init; }
}
