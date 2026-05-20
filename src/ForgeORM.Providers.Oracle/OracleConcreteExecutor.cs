using System.Data;
using System.Data.Common;
using ForgeORM.Core;

namespace ForgeORM.Providers.Oracle;

public sealed class OracleConcreteExecutor : IForgeProviderExecutor
{
    public string ProviderName => "Oracle";

    public ValueTask<DbCommand> CreateCommandAsync(DbConnection connection, string sql, object? parameters, DbTransaction? transaction, CommandType commandType, int? timeoutSeconds, CancellationToken cancellationToken)
        => ValueTask.FromResult(ForgeAdo.CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds));

    public async IAsyncEnumerable<T> StreamAsync<T>(DbConnection connection, string sql, object? parameters, DbTransaction? transaction, CommandType commandType, int? timeoutSeconds, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
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
