using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

public static partial class ForgeAdoStreamingExtensions
{
    public static async IAsyncEnumerable<T> StreamAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeIlMaterializerCache.GetOrCreate<T>(reader);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return materializer(reader);
    }
}
