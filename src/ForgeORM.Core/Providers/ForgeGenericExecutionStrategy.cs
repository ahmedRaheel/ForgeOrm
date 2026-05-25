using System.Data.Common;

namespace ForgeORM.Core;

internal sealed class ForgeGenericExecutionStrategy : IForgeProviderExecutionStrategy
{
    public static readonly ForgeGenericExecutionStrategy Instance = new();
    private ForgeGenericExecutionStrategy() { }
    public string ProviderName => "Generic";
    public string BulkInsertStrategy => "Batched parameterized INSERT";
    public string BulkUpdateStrategy => "Batched parameterized UPDATE";
    public string BulkDeleteStrategy => "Batched parameterized DELETE";
    public string GraphWriteStrategy => "Batched transaction graph write";
    public ValueTask<int> ExecuteBulkInsertAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
        => ForgeProviderBulkFallback.InsertRowsAsync(connection, rows, cancellationToken);
}
