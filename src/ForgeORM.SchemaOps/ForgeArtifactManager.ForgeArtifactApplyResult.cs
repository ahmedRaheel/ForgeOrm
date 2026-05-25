using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.QueryAst.Artifacts;

namespace ForgeORM.SchemaOps;

public sealed class ForgeArtifactApplyResult
{
    public required string ArtifactName { get; init; }
    public required string Schema { get; init; }
    public required ForgeDbArtifactType ArtifactType { get; init; }
    public required int VersionNo { get; init; }
    public required bool Applied { get; init; }
    public required bool SkippedBecauseUnchanged { get; init; }
    public string? SqlHash { get; init; }
}
