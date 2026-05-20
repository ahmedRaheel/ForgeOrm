using System.Data;
using System.Data.Common;
using ForgeORM.Core;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// SQL Server concrete-driver execution path. Keeps public APIs provider-neutral while allowing
/// ForgeORM internals to avoid generic provider branches for SQL Server workloads.
/// </summary>
public sealed class SqlServerConcreteExecutor : IForgeProviderExecutor
{
    public string ProviderName => "SqlServer";

    public ValueTask<DbCommand> CreateCommandAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (connection is not SqlConnection sqlConnection)
            return ValueTask.FromResult(ForgeAdo.CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds));

        var command = sqlConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = commandType;
        if (timeoutSeconds.HasValue) command.CommandTimeout = timeoutSeconds.Value;
        if (transaction is SqlTransaction sqlTransaction) command.Transaction = sqlTransaction;

        // Central binder keeps scalar @Id, anonymous objects and generated binders consistent.
        var generic = ForgeAdo.CreateCommand(sqlConnection, sql, parameters, command.Transaction, commandType, timeoutSeconds);
        return ValueTask.FromResult((DbCommand)generic);
    }

    public async IAsyncEnumerable<T> StreamAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeMaterializer.GetReader<T>(reader);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return materializer(reader);
    }
}
