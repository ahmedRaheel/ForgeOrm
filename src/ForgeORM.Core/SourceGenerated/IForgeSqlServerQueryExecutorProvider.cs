using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

/// <summary>
/// Source-generated, whole-query SQL Server executor. This is the fastest ForgeORM path:
/// generated code owns command text, parameter binding, execution behavior and materialization.
/// The generic runtime pipeline remains the fallback for dynamic SQL.
/// </summary>
public interface IForgeSqlServerQueryExecutorProvider
{
    bool TryFirstOrDefaultAsync<T>(
        string sql,
        SqlConnection connection,
        object? parameters,
        SqlTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result);

    bool TryQueryAsync<T>(
        string sql,
        SqlConnection connection,
        object? parameters,
        SqlTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<IReadOnlyList<T>> result);
}
