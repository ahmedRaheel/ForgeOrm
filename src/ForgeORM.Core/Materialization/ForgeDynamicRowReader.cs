using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace ForgeORM.Core.Materialization;

/// <summary>
/// Central low-level dynamic row reader.
/// </summary>
public static class ForgeDynamicRowReader
{
    public static async ValueTask<IReadOnlyList<Dictionary<string, object?>>> ReadDictionaryAsync(
        DbCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

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
}
