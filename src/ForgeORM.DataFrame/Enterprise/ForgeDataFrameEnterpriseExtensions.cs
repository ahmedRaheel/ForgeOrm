using System.Text;

namespace ForgeORM.DataFrame;

/// <summary>
/// Enterprise DataFrame operations inspired by pandas but implemented without external services.
/// </summary>
public static class ForgeDataFrameEnterpriseExtensions
{
    public static ForgeDataFrame Join(this ForgeDataFrame left, ForgeDataFrame right, string key)
    {
        var rightLookup = right.Rows.GroupBy(r => r.TryGetValue(key, out var v) ? v : null).ToDictionary(g => g.Key, g => g.ToList());
        var output = new List<IDictionary<string, object?>>();
        foreach (var row in left.Rows)
        {
            row.TryGetValue(key, out var keyValue);
            if (!rightLookup.TryGetValue(keyValue, out var matches)) continue;
            foreach (var match in matches)
            {
                var merged = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
                foreach (var col in match) merged[col.Key] = col.Value;
                output.Add(merged);
            }
        }
        return new ForgeDataFrame(output);
    }

    public static ForgeDataFrame FillNull(this ForgeDataFrame frame, object? value)
    {
        return new ForgeDataFrame(frame.Rows.Select(row => row.ToDictionary(x => x.Key, x => x.Value ?? value, StringComparer.OrdinalIgnoreCase)));
    }

    public static ForgeDataFrame DropDuplicates(this ForgeDataFrame frame, params string[] columns)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<IDictionary<string, object?>>();
        foreach (var row in frame.Rows)
        {
            var key = string.Join("|", (columns.Length == 0 ? frame.Columns : columns).Select(c => row.TryGetValue(c, out var v) ? v : null));
            if (seen.Add(key)) output.Add(new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase));
        }
        return new ForgeDataFrame(output);
    }

    public static ForgeDataFrame NormalizeColumn(this ForgeDataFrame frame, string column, string alias)
    {
        var values = frame.Rows.Select(r => r.TryGetValue(column, out var v) ? ForgeDataFrame.ToDecimal(v) : null).Where(x => x.HasValue).Select(x => x!.Value).ToList();
        if (values.Count == 0) return frame;
        var min = values.Min();
        var max = values.Max();
        var range = max - min;
        return new ForgeDataFrame(frame.Rows.Select(row =>
        {
            var copy = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
            var value = row.TryGetValue(column, out var v) ? ForgeDataFrame.ToDecimal(v) : null;
            copy[alias] = value.HasValue && range != 0 ? (value.Value - min) / range : 0;
            return copy;
        }));
    }

    public static ForgeDataFrame MovingAverage(this ForgeDataFrame frame, string valueColumn, int window, string alias)
    {
        var rows = frame.Rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            var slice = rows.Skip(Math.Max(0, i - window + 1)).Take(Math.Min(window, i + 1))
                .Select(r => r.TryGetValue(valueColumn, out var v) ? ForgeDataFrame.ToDecimal(v) : null)
                .Where(x => x.HasValue).Select(x => x!.Value).ToList();
            rows[i][alias] = slice.Count == 0 ? null : slice.Average();
        }
        return new ForgeDataFrame(rows);
    }

    public static ForgeDataFrame DetectOutliers(this ForgeDataFrame frame, string column, string alias = "IsOutlier")
    {
        var values = frame.Rows.Select(r => r.TryGetValue(column, out var v) ? ForgeDataFrame.ToDecimal(v) : null).Where(x => x.HasValue).Select(x => x!.Value).OrderBy(x => x).ToList();
        if (values.Count < 4) return frame;
        var q1 = ForgeDataFrame.Percentile(values, 0.25m);
        var q3 = ForgeDataFrame.Percentile(values, 0.75m);
        var iqr = q3 - q1;
        var low = q1 - 1.5m * iqr;
        var high = q3 + 1.5m * iqr;
        return new ForgeDataFrame(frame.Rows.Select(row =>
        {
            var copy = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
            var v = row.TryGetValue(column, out var raw) ? ForgeDataFrame.ToDecimal(raw) : null;
            copy[alias] = v.HasValue && (v.Value < low || v.Value > high);
            return copy;
        }));
    }

    public static decimal? Correlation(this ForgeDataFrame frame, string xColumn, string yColumn)
    {
        var pairs = frame.Rows.Select(r => new
        {
            X = r.TryGetValue(xColumn, out var x) ? ForgeDataFrame.ToDecimal(x) : null,
            Y = r.TryGetValue(yColumn, out var y) ? ForgeDataFrame.ToDecimal(y) : null
        }).Where(p => p.X.HasValue && p.Y.HasValue).Select(p => (X: p.X!.Value, Y: p.Y!.Value)).ToList();
        if (pairs.Count < 2) return null;
        var avgX = pairs.Average(p => p.X);
        var avgY = pairs.Average(p => p.Y);
        var numerator = pairs.Sum(p => (p.X - avgX) * (p.Y - avgY));
        var denomX = (decimal)Math.Sqrt((double)pairs.Sum(p => (p.X - avgX) * (p.X - avgX)));
        var denomY = (decimal)Math.Sqrt((double)pairs.Sum(p => (p.Y - avgY) * (p.Y - avgY)));
        return denomX == 0 || denomY == 0 ? null : numerator / (denomX * denomY);
    }

    public static string ExportCsvText(this ForgeDataFrame frame)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", frame.Columns));
        foreach (var row in frame.Rows)
        {
            sb.AppendLine(string.Join(",", frame.Columns.Select(c => Escape(row.TryGetValue(c, out var v) ? v : null))));
        }
        return sb.ToString();
    }

    public static string ExportHtmlTable(this ForgeDataFrame frame)
    {
        var sb = new StringBuilder("<table><thead><tr>");
        foreach (var col in frame.Columns) sb.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(col)).Append("</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var row in frame.Rows)
        {
            sb.Append("<tr>");
            foreach (var col in frame.Columns) sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.TryGetValue(col, out var v) ? v?.ToString() : string.Empty)).Append("</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string Escape(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        return text.Contains(',') || text.Contains('"') || text.Contains('\n') ? '"' + text.Replace("\"", "\"\"") + '"' : text;
    }
}
