//using System.Data.Common;
//using System.Text;
//using System.Text.Json;

//namespace ForgeORM.Core.Materialization;

///// <summary>
///// JSON-friendly query result used by reports, dynamic queries and analytics projections.
///// </summary>
//public sealed class ForgeJsonProjection
//{
//    public string? Name { get; init; }

//    public string Sql { get; init; } = string.Empty;

//    public int RowCount { get; init; }

//    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } =
//        Array.Empty<Dictionary<string, object?>>();

//    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
//}

///// <summary>
///// Lightweight DataFrame-friendly projection that can be returned from reports and queries.
///// </summary>
//public sealed class ForgeTabularResult
//{
//    public string? Name { get; init; }

//    public IReadOnlyList<string> Columns { get; init; } =
//        Array.Empty<string>();

//    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } =
//        Array.Empty<Dictionary<string, object?>>();

//    public int RowCount => Rows.Count;

//    public string ToCsv()
//    {
//        if (Rows.Count == 0)
//        {
//            return string.Empty;
//        }

//        var columns = Columns.Count > 0
//            ? Columns
//            : Rows.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

//        var builder = new StringBuilder();
//        builder.AppendLine(string.Join(",", columns.Select(Escape)));

//        foreach (var row in Rows)
//        {
//            builder.AppendLine(string.Join(",", columns.Select(column =>
//                Escape(row.TryGetValue(column, out var value) ? value : null))));
//        }

//        return builder.ToString();

//        static string Escape(object? value)
//        {
//            var text = value?.ToString() ?? string.Empty;
//            return text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r')
//                ? "\"" + text.Replace("\"", "\"\"") + "\""
//                : text;
//        }
//    }
//}

///// <summary>
///// Central low-level dynamic row reader. Any dynamic report/pivot/unpivot/analytics query should use this instead of POCO mapping.
///// </summary>
//public static class ForgeDynamicRowReader
//{
//    public static async Task<IReadOnlyList<Dictionary<string, object?>>> ReadDictionaryAsync(
//        DbCommand command,
//        CancellationToken cancellationToken = default)
//    {
//        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
//        var rows = new List<Dictionary<string, object?>>();

//        while (await reader.ReadAsync(cancellationToken))
//        {
//            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

//            for (var i = 0; i < reader.FieldCount; i++)
//            {
//                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken)
//                    ? null
//                    : reader.GetValue(i);
//            }

//            rows.Add(row);
//        }

//        return rows;
//    }
//}

///// <summary>
///// Serialization helpers used by ToJsonAsync / ToCsvAsync terminal APIs.
///// </summary>
//public static class ForgeMaterializationSerializer
//{
//    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
//    {
//        WriteIndented = true
//    };

//    public static string ToJson(object value)
//    {
//        return JsonSerializer.Serialize(value, JsonOptions);
//    }

//    public static string ToCsv(IReadOnlyList<Dictionary<string, object?>> rows)
//    {
//        return new ForgeTabularResult
//        {
//            Columns = rows.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
//            Rows = rows
//        }.ToCsv();
//    }
//}
