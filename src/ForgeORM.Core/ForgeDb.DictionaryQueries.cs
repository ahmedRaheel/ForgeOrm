using System.Data;
using System.Data.Common;

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
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = ForgeAdo.CreateCommand(
            connection,
            sql,
            parameters,
            transaction: null,
            commandType: CommandType.Text,
            timeoutSeconds: timeoutSeconds);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken)
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
