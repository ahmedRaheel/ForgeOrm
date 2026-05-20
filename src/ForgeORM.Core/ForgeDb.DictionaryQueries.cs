using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Executes a query and returns each row as a case-insensitive dictionary.
    /// This is the correct materialization path for dynamic reports, pivots, unpivots,
    /// analytics projections, and SQL that does not map to a static entity type.
    /// </summary>
    public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryDictionaryAsync(
        string sql,
        object? parameters = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
        {
            return await ForgeSqlServerProviderDirectHotPath.QueryDictionaryAsync(
                _connectionString,
                sql,
                parameters,
                timeoutSeconds,
                cancellationToken).ConfigureAwait(false);
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = ForgeAdo.CreateCommand(
            connection,
            sql,
            parameters,
            transaction: null,
            commandType: CommandType.Text,
            timeoutSeconds: timeoutSeconds);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);

        var rows = new List<Dictionary<string, object?>>(16);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false)
                    ? null
                    : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Synchronous dictionary query helper for dynamic report rows.
    /// </summary>
    public IReadOnlyList<Dictionary<string, object?>> QueryDictionary(
        string sql,
        object? parameters = null,
        int? timeoutSeconds = null)
        => QueryDictionaryAsync(sql, parameters, timeoutSeconds).GetAwaiter().GetResult();
}
