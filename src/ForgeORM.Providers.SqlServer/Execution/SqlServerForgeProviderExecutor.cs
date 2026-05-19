using System.Data;
using System.Data.Common;
using ForgeORM.Core;

namespace ForgeORM.Providers.SqlServer.Execution;

public sealed class SqlServerForgeProviderExecutor : IForgeProviderExecutor
{
    private readonly ForgeDefaultProviderExecutor _inner = new("SqlServer");
    public string ProviderName => "SqlServer";

    public ValueTask<DbCommand> CreateCommandAsync(DbConnection connection, string sql, object? parameters, DbTransaction? transaction, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
        => _inner.CreateCommandAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);

    public IAsyncEnumerable<T> StreamAsync<T>(DbConnection connection, string sql, object? parameters, DbTransaction? transaction, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
        => _inner.StreamAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken);
}
