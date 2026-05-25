using System.Data.Common;

namespace ForgeORM.Core;

internal sealed class ForgeMySqlExecutionStrategy : IForgeProviderExecutionStrategy
{
    public static readonly ForgeMySqlExecutionStrategy Instance = new();
    private ForgeMySqlExecutionStrategy() { }
    public string ProviderName => "MySql";
    public string BulkInsertStrategy => "Multi-row INSERT";
    public string BulkUpdateStrategy => "Multi-row INSERT + ON DUPLICATE KEY UPDATE";
    public string BulkDeleteStrategy => "Temporary keys + DELETE JOIN";
    public string GraphWriteStrategy => "Multi-row batched graph write";
    public ValueTask<int> ExecuteBulkInsertAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
        => ForgeProviderBulkFallback.InsertRowsAsync(connection, rows, cancellationToken);
}
