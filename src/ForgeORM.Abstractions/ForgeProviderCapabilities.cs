using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgeProviderCapabilities
{
    public bool SupportsBulkInsert { get; init; }
    public bool SupportsBulkUpdate { get; init; }
    public bool SupportsBulkDelete { get; init; }
    public bool SupportsBulkMerge { get; init; }
    public bool SupportsStoredProcedures { get; init; }
    public bool SupportsFunctions { get; init; }
    public bool SupportsTableValuedParameters { get; init; }
    public bool SupportsArrayParameters { get; init; }
    public bool SupportsCopy { get; init; }
    public bool SupportsReturningClause { get; init; }
    public bool SupportsJsonColumns { get; init; }
    public bool SupportsRefCursor { get; init; }
}
