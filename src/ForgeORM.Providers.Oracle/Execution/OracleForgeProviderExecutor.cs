using System.Data;
using System.Data.Common;
using ForgeORM.Core;

namespace ForgeORM.Providers.Oracle.Execution;

public sealed class OracleForgeProviderExecutor : IForgeProviderExecutor
{
    private readonly ForgeDefaultProviderExecutor _inner = new("Oracle");
    public string ProviderName => "Oracle";

    public ValueTask<DbCommand> CreateCommandAsync(DbConnection connection, string sql, object? parameters, DbTransaction? transaction, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
        => _inner.CreateCommandAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);

    public IAsyncEnumerable<T> StreamAsync<T>(DbConnection connection, string sql, object? parameters, DbTransaction? transaction, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
        => _inner.StreamAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
}
