using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace ForgeORM.Core.Materialization;

/// <summary>
/// Lightweight DataFrame-friendly projection that can be returned from reports and queries.
/// </summary>
public sealed class ForgeTabularResult
{
    public string? Name { get; init; }

    public IReadOnlyList<string> Columns { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } =
        Array.Empty<Dictionary<string, object?>>();

    public int RowCount => Rows.Count;

    public string ToCsv()
    {
        if (Rows.Count == 0)
        {
            return string.Empty;
        }

        var columns = Columns.Count > 0
            ? Columns
            : Rows.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", columns.Select(Escape)));

        foreach (var row in Rows)
        {
            builder.AppendLine(string.Join(",", columns.Select(column =>
                Escape(row.TryGetValue(column, out var value) ? value : null))));
        }

        return builder.ToString();

        static string Escape(object? value)
        {
            var text = value?.ToString() ?? string.Empty;

            return text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r')
                ? "\"" + text.Replace("\"", "\"\"") + "\""
                : text;
        }
    }
}
