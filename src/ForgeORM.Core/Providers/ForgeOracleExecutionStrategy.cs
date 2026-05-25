using System.Data.Common;

namespace ForgeORM.Core;

internal sealed class ForgeOracleExecutionStrategy : IForgeProviderExecutionStrategy
{
    public static readonly ForgeOracleExecutionStrategy Instance = new();
    private ForgeOracleExecutionStrategy() { }
    public string ProviderName => "Oracle";
    public string BulkInsertStrategy => "Array binding";
    public string BulkUpdateStrategy => "Array binding + MERGE";
    public string BulkDeleteStrategy => "Array binding keys + DELETE";
    public string GraphWriteStrategy => "Array binding graph batches";
    public ValueTask<int> ExecuteBulkInsertAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
        => ForgeProviderBulkFallback.InsertRowsAsync(connection, rows, cancellationToken);
}
