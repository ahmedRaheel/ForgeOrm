using System.Data.Common;

namespace ForgeORM.Core;

internal sealed class ForgeSqlServerExecutionStrategy : IForgeProviderExecutionStrategy
{
    public static readonly ForgeSqlServerExecutionStrategy Instance = new();
    private ForgeSqlServerExecutionStrategy() { }
    public string ProviderName => "SqlServer";
    public string BulkInsertStrategy => "SqlBulkCopy";
    public string BulkUpdateStrategy => "TVP + MERGE";
    public string BulkDeleteStrategy => "TVP + DELETE JOIN";
    public string GraphWriteStrategy => "Temp table + SqlBulkCopy + MERGE";
    public ValueTask<int> ExecuteBulkInsertAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
        => ForgeProviderBulkFallback.InsertRowsAsync(connection, rows, cancellationToken);
}
