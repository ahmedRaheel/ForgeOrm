using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Advanced transaction policy foundation.
/// </summary>
public sealed record ForgeAdvancedTransactionPolicy(
    bool EnableRetry,
    bool EnableIdempotency,
    bool UseOutbox,
    int MaxRetries = 3);
