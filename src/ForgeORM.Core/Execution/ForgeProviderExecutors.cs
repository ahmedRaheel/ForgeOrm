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

public sealed class ForgeDefaultProviderExecutor : IForgeProviderExecutor
{
    public string ProviderName { get; }

    public ForgeDefaultProviderExecutor(string providerName = "Default")
    {
        ProviderName = providerName;
    }

    public ValueTask<DbCommand> CreateCommandAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(ForgeAdo.CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds));
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
        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeMaterializer.GetReader<T>(reader);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return materializer(reader);
    }
}
