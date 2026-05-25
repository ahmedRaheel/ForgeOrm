using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Provider-specific hot-path strategy selector. It avoids one generic path doing all provider work.
/// Concrete optimized paths can be extended without changing public db methods.
/// </summary>
public interface IForgeProviderExecutionStrategy
{
    string ProviderName { get; }
    string BulkInsertStrategy { get; }
    string BulkUpdateStrategy { get; }
    string BulkDeleteStrategy { get; }
    string GraphWriteStrategy { get; }
    ValueTask<int> ExecuteBulkInsertAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default);
}
