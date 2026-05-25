using System.Data;
using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Provider-specific hot-path executor contract. Each provider can override command setup,
/// bulk write, TVP/temp-table merge, timeout, and transaction details without changing public APIs.
/// </summary>
public interface IForgeProviderExecutor
{
    string ProviderName { get; }

    ValueTask<DbCommand> CreateCommandAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken);

    IAsyncEnumerable<T> StreamAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken);
}
