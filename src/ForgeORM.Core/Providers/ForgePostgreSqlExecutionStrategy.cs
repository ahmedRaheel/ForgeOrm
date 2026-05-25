using System.Data.Common;

namespace ForgeORM.Core;

internal sealed class ForgePostgreSqlExecutionStrategy : IForgeProviderExecutionStrategy
{
    public static readonly ForgePostgreSqlExecutionStrategy Instance = new();
    private ForgePostgreSqlExecutionStrategy() { }
    public string ProviderName => "PostgreSql";
    public string BulkInsertStrategy => "COPY";
    public string BulkUpdateStrategy => "COPY temp + ON CONFLICT";
    public string BulkDeleteStrategy => "UNNEST keys + DELETE USING";
    public string GraphWriteStrategy => "COPY temp + ON CONFLICT graph batches";
    public ValueTask<int> ExecuteBulkInsertAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
        => ForgeProviderBulkFallback.InsertRowsAsync(connection, rows, cancellationToken);
}
