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

public static class ForgeProviderExecutionStrategySelector
{
    public static IForgeProviderExecutionStrategy Resolve(DbConnection connection)
    {
        var name = connection.GetType().FullName ?? connection.GetType().Name;
        if (name.Contains("SqlClient", StringComparison.OrdinalIgnoreCase))
            return ForgeSqlServerExecutionStrategy.Instance;
        if (name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || name.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
            return ForgePostgreSqlExecutionStrategy.Instance;
        if (name.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            return ForgeMySqlExecutionStrategy.Instance;
        if (name.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            return ForgeOracleExecutionStrategy.Instance;
        return ForgeGenericExecutionStrategy.Instance;
    }
}

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
