using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public enum ForgeDbArtifactType
{
    View,
    StoredProcedure,
    Function,
    Script
}
