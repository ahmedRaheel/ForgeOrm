namespace ForgeORM.Core.DataFrame;

public sealed class ForgeEnterpriseDataFrame
{
    private readonly List<Dictionary<string, object?>> _rows = [];

    public IReadOnlyList<Dictionary<string, object?>> Rows => _rows;
    public int RowCount => _rows.Count;

    public ForgeEnterpriseDataFrame AddRow(Dictionary<string, object?> row)
    {
        _rows.Add(row);
        return this;
    }

    public ForgeEnterpriseDataFrame FillNull(string column, object? value)
    {
        foreach (var row in _rows)
        {
            if (!row.TryGetValue(column, out var current) || current is null || current is DBNull)
            {
                row[column] = value;
            }
        }

        return this;
    }

    public ForgeEnterpriseDataFrame DropDuplicates(params string[] columns)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = new List<Dictionary<string, object?>>();

        foreach (var row in _rows)
        {
            var key = string.Join("|", columns.Select(c => row.TryGetValue(c, out var v) ? v?.ToString() ?? "" : ""));
            if (seen.Add(key))
            {
                deduped.Add(row);
            }
        }

        _rows.Clear();
        _rows.AddRange(deduped);
        return this;
    }

    public ForgeEnterpriseDataFrame Join(
        ForgeEnterpriseDataFrame right,
        string leftKey,
        string rightKey,
        string prefix = "Right_")
    {
        var lookup = right.Rows
            .Where(r => r.ContainsKey(rightKey))
            .GroupBy(r => r[rightKey]?.ToString() ?? "", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = new ForgeEnterpriseDataFrame();

        foreach (var left in _rows)
        {
            var copy = new Dictionary<string, object?>(left, StringComparer.OrdinalIgnoreCase);
            var key = left.TryGetValue(leftKey, out var value) ? value?.ToString() ?? "" : "";

            if (lookup.TryGetValue(key, out var rightRow))
            {
                foreach (var kv in rightRow)
                {
                    if (!kv.Key.Equals(rightKey, StringComparison.OrdinalIgnoreCase))
                    {
                        copy[prefix + kv.Key] = kv.Value;
                    }
                }
            }

            result.AddRow(copy);
        }

        return result;
    }

    public Dictionary<string, decimal> CorrelationReadySummary(string xColumn, string yColumn)
    {
        var pairs = _rows
            .Where(r => r.ContainsKey(xColumn) && r.ContainsKey(yColumn))
            .Select(r => (X: Convert.ToDecimal(r[xColumn]), Y: Convert.ToDecimal(r[yColumn])))
            .ToList();

        if (pairs.Count == 0)
        {
            return new Dictionary<string, decimal> { ["Count"] = 0 };
        }

        return new Dictionary<string, decimal>
        {
            ["Count"] = pairs.Count,
            ["SumX"] = pairs.Sum(p => p.X),
            ["SumY"] = pairs.Sum(p => p.Y),
            ["AvgX"] = pairs.Average(p => p.X),
            ["AvgY"] = pairs.Average(p => p.Y)
        };
    }

    public IReadOnlyList<Dictionary<string, object?>> RollingAverage(string sourceColumn, string targetColumn, int window)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var start = Math.Max(0, i - window + 1);
            var values = _rows.Skip(start).Take(i - start + 1)
                .Where(r => r.TryGetValue(sourceColumn, out var value) && value is not null)
                .Select(r => Convert.ToDecimal(r[sourceColumn]))
                .ToList();

            _rows[i][targetColumn] = values.Count == 0 ? 0 : values.Average();
        }

        return _rows;
    }

    public string ToCsv()
    {
        if (_rows.Count == 0) return string.Empty;

        var columns = _rows.SelectMany(r => r.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var lines = new List<string> { string.Join(",", columns) };

        foreach (var row in _rows)
        {
            lines.Add(string.Join(",", columns.Select(c => Escape(row.TryGetValue(c, out var v) ? v : null))));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Escape(object? value)
    {
        var text = value?.ToString() ?? "";
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? "\"" + text.Replace("\"", "\"\"") + "\""
            : text;
    }
}
