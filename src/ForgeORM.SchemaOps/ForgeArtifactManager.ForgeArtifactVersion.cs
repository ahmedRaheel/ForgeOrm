using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.QueryAst.Artifacts;

namespace ForgeORM.SchemaOps;

internal sealed class ForgeArtifactVersion
{
    public long Id { get; init; }
    public string ArtifactType { get; init; } = "";
    public string SchemaName { get; init; } = "";
    public string ArtifactName { get; init; } = "";
    public int VersionNo { get; init; }
    public string SqlHash { get; init; } = "";
    public string SqlDefinition { get; init; } = "";
}
