using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    private ForgeDbAnalysisFacade? _analysis;

    /// <summary>
    /// Database diagnostics, SQL analysis, index-advice and query profile facade.
    /// </summary>
    public ForgeDbAnalysisFacade Analysis => _analysis ??= new ForgeDbAnalysisFacade(this);
}
