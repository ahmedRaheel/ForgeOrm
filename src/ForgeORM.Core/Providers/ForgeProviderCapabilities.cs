using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

/// <summary>
/// Provider-specific bulk/graph/query capability matrix.
/// </summary>
public sealed record ForgeProviderCapabilities(
    ForgePhysicalProvider Provider,
    bool SupportsMerge,
    bool SupportsJsonBulk,
    bool SupportsStructuredParameters,
    bool SupportsCopy,
    bool SupportsReturning,
    bool SupportsSkipLocked,
    bool SupportsNoLock,
    bool SupportsKeysetPaging);
