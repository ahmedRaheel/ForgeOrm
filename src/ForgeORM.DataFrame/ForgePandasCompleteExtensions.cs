using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

/// <summary>
/// Complete pandas-inspired facade for ForgeDataFrame. The methods are intentionally pragmatic:
/// they provide the same analytics workflow surface as pandas while staying dependency-light and ORM friendly.
/// </summary>
public static partial class ForgePandas
{
    public static ForgeDataFrame FromDict(IDictionary<string, IEnumerable<object?>> columns) => DataFrame(columns);

    public static ForgeDataFrame JsonNormalize(string json, string? recordPath = null)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = new List<IDictionary<string, object?>>();
        var root = doc.RootElement;
        if (!string.IsNullOrWhiteSpace(recordPath))
        {
            foreach (var part in recordPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                root = root.GetProperty(part);
        }
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray()) rows.Add(FlattenJson(item));
        }
        else rows.Add(FlattenJson(root));
        return new ForgeDataFrame(rows);
    }

    public static ForgeSeries DateRange(DateTime start, DateTime end, TimeSpan? frequency = null, string name = "Date")
    {
        var step = frequency ?? TimeSpan.FromDays(1);
        var values = new List<object?>();
        for (var current = start; current <= end; current = current.Add(step)) values.Add(current);
        return new ForgeSeries(name, values);
    }

    public static ForgeSeries PeriodRange(DateTime start, DateTime end, string frequency = "M", string name = "Period")
    {
        var values = new List<object?>();
        for (var current = start; current <= end; current = frequency.Equals("Y", StringComparison.OrdinalIgnoreCase) ? current.AddYears(1) : current.AddMonths(1))
            values.Add(frequency.Equals("Y", StringComparison.OrdinalIgnoreCase) ? current.ToString("yyyy", CultureInfo.InvariantCulture) : current.ToString("yyyy-MM", CultureInfo.InvariantCulture));
        return new ForgeSeries(name, values);
    }

    public static ForgeSeries TimedeltaRange(TimeSpan start, TimeSpan end, TimeSpan? frequency = null, string name = "Timedelta")
    {
        var step = frequency ?? TimeSpan.FromDays(1);
        var values = new List<object?>();
        for (var current = start; current <= end; current += step) values.Add(current);
        return new ForgeSeries(name, values);
    }

    public static DateTimeOffset Timestamp(object? value) => ToDateTime(value) ?? DateTimeOffset.MinValue;

    public static TimeSpan Timedelta(object? value) => ToTimedelta(value) ?? TimeSpan.Zero;

    public static TimeSpan? ToTimedelta(object? value)
    {
        if (value is null or DBNull) return null;
        if (value is TimeSpan ts) return ts;
        if (value is int i) return TimeSpan.FromDays(i);
        if (value is long l) return TimeSpan.FromTicks(l);
        return TimeSpan.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public static ForgeDataFrame ReadTable(string path, char delimiter = '\t', bool hasHeader = true) => ForgeDataFrame.FromCsv(path, hasHeader, delimiter);

    public static ForgeDataFrame ReadFwf(string path, params (string Name, int Start, int Length)[] columns)
    {
        var rows = new List<IDictionary<string, object?>>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                var text = line.Length <= column.Start ? string.Empty : line.Substring(column.Start, Math.Min(column.Length, line.Length - column.Start)).Trim();
                row[column.Name] = Coerce(text);
            }
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    public static ForgeDataFrame ReadJson(string path) => ForgeDataFrame.FromJson(path);

    public static ValueTask<ForgeDataFrame> ReadJsonAsync(string path, CancellationToken cancellationToken = default) => ForgeDataFrame.FromJsonAsync(path, cancellationToken);

    public static async ValueTask<ForgeDataFrame> ReadSqlAsync(ForgeDb db, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var table = await db.QueryDataFrameAsync(sql, parameters, cancellationToken: cancellationToken);
        return new ForgeDataFrame(table.Rows);
    }

    public static async ValueTask<ForgeDataFrame> ReadSqlAsync(ForgeDbContext db, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var table = await db.QueryDataFrameAsync(sql, parameters, cancellationToken: cancellationToken);
        return new ForgeDataFrame(table.Rows);
    }

    public static ForgeDataFrame ReadHtml(string html)
    {
        var rows = new List<IDictionary<string, object?>>();
        var tableStart = html.IndexOf("<table", StringComparison.OrdinalIgnoreCase);
        if (tableStart < 0) return ForgeDataFrame.Empty;
        var headers = ExtractCells(html, "th");
        var trParts = html.Split(new[] { "<tr", "<TR" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var tr in trParts)
        {
            var cells = ExtractCells(tr, "td");
            if (cells.Count == 0) continue;
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < cells.Count; i++) row[i < headers.Count ? headers[i] : $"Column{i + 1}"] = Coerce(cells[i]);
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    public static ForgeDataFrame ReadXml(string xml, string rowElementName = "row")
    {
        var doc = XDocument.Parse(xml);
        var rows = new List<IDictionary<string, object?>>();
        foreach (var node in doc.Descendants(rowElementName))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in node.Attributes()) row[attr.Name.LocalName] = Coerce(attr.Value);
            foreach (var element in node.Elements()) row[element.Name.LocalName] = Coerce(element.Value);
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    public static ForgeDataFrame ReadPickle(string path) => ReadJson(path); // portable ForgeORM equivalent
    public static void ToPickle(ForgeDataFrame frame, string path) => frame.ToJson(path);

    public static (decimal[] Bins, int[] Codes) Cut(IEnumerable<decimal> values, int bins)
    {
        var arr = values.ToArray();
        if (arr.Length == 0 || bins <= 0) return ([], []);
        var min = arr.Min(); var max = arr.Max(); var width = (max - min) / bins;
        if (width == 0) width = 1;
        var edges = Enumerable.Range(0, bins + 1).Select(i => min + width * i).ToArray();
        var codes = arr.Select(v => Math.Min(bins - 1, Math.Max(0, (int)((v - min) / width)))).ToArray();
        return (edges, codes);
    }

    public static (decimal[] Quantiles, int[] Codes) QCut(IEnumerable<decimal> values, int q)
    {
        var ordered = values.OrderBy(x => x).ToArray();
        if (ordered.Length == 0 || q <= 0) return ([], []);
        var edges = new decimal[q + 1];
        for (var i = 0; i <= q; i++) edges[i] = Percentile(ordered, (decimal)i / q);
        var codes = ordered.Select(v => Math.Min(q - 1, Math.Max(0, Array.FindLastIndex(edges, e => v >= e)))).ToArray();
        return (edges, codes);
    }

    public static (IReadOnlyList<object?> Values, int[] Codes) Factorize(IEnumerable<object?> values)
    {
        var uniques = new List<object?>();
        var codes = new List<int>();
        foreach (var value in values)
        {
            var index = uniques.FindIndex(x => Same(x, value));
            if (index < 0) { uniques.Add(value); index = uniques.Count - 1; }
            codes.Add(index);
        }
        return (uniques, codes.ToArray());
    }

    public static IReadOnlyList<object?> Unique(IEnumerable<object?> values)
    {
        var list = new List<object?>();
        foreach (var value in values) if (!list.Any(x => Same(x, value))) list.Add(value);
        return list;
    }

    public static ForgeDataFrame ValueCounts(IEnumerable<object?> values, string column = "Value") => new ForgeSeries(column, values).ValueCounts();

    public static ForgeMultiIndex MultiIndexFromArrays(params IEnumerable<object?>[] arrays) => ForgeMultiIndex.FromArrays(arrays);
    public static ForgeMultiIndex MultiIndexFromTuples(params object?[][] tuples) => ForgeMultiIndex.FromTuples(tuples);
    public static ForgeMultiIndex MultiIndexFromProduct(params IEnumerable<object?>[] levels) => ForgeMultiIndex.FromProduct(levels);

    public static ForgeInterval Interval(decimal left, decimal right, bool closedLeft = true, bool closedRight = false) => new(left, right, closedLeft, closedRight);
    public static IReadOnlyList<ForgeInterval> IntervalRange(decimal start, decimal end, decimal frequency)
    {
        var list = new List<ForgeInterval>();
        for (var current = start; current < end; current += frequency) list.Add(new ForgeInterval(current, Math.Min(end, current + frequency)));
        return list;
    }

    private static Dictionary<string, object?> FlattenJson(JsonElement element, string prefix = "")
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (element.ValueKind != JsonValueKind.Object) { row[prefix.Trim('.').Length == 0 ? "Value" : prefix.Trim('.')] = JsonValue(element); return row; }
        foreach (var prop in element.EnumerateObject())
        {
            var name = string.IsNullOrWhiteSpace(prefix) ? prop.Name : prefix + "." + prop.Name;
            if (prop.Value.ValueKind == JsonValueKind.Object)
                foreach (var kv in FlattenJson(prop.Value, name)) row[kv.Key] = kv.Value;
            else row[name] = JsonValue(prop.Value);
        }
        return row;
    }

    private static object? JsonValue(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.TryGetDecimal(out var d) ? d : e.GetDouble(),
        JsonValueKind.String => e.TryGetDateTimeOffset(out var dto) ? dto : e.GetString(),
        _ => e.ToString()
    };

    private static IReadOnlyList<string> ExtractCells(string html, string tag)
    {
        var list = new List<string>(); var startTag = "<" + tag; var endTag = "</" + tag + ">"; var index = 0;
        while ((index = html.IndexOf(startTag, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var close = html.IndexOf('>', index); if (close < 0) break;
            var end = html.IndexOf(endTag, close + 1, StringComparison.OrdinalIgnoreCase); if (end < 0) break;
            list.Add(System.Net.WebUtility.HtmlDecode(StripTags(html[(close + 1)..end])).Trim());
            index = end + endTag.Length;
        }
        return list;
    }

    private static string StripTags(string text)
    {
        var sb = new StringBuilder(text.Length); var inside = false;
        foreach (var c in text) { if (c == '<') inside = true; else if (c == '>') inside = false; else if (!inside) sb.Append(c); }
        return sb.ToString();
    }

    internal static object? Coerce(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        if (bool.TryParse(text, out var b)) return b;
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dto)) return dto;
        return text;
    }

    private static decimal Percentile(IReadOnlyList<decimal> ordered, decimal p)
    {
        if (ordered.Count == 0) return 0; if (ordered.Count == 1) return ordered[0];
        var position = (ordered.Count - 1) * p; var lower = (int)Math.Floor(position); var upper = (int)Math.Ceiling(position);
        return lower == upper ? ordered[lower] : ordered[lower] + (ordered[upper] - ordered[lower]) * (position - lower);
    }

    private static bool Same(object? a, object? b) => string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
}

public static partial class ForgePandasExtensions
{
    public static int Size(this ForgeDataFrame frame) => frame.RowCount * frame.Columns.Count;
    public static int NDim(this ForgeDataFrame frame) => 2;
    public static IReadOnlyList<string> DTypes(this ForgeDataFrame frame) => frame.Columns.Select(c => $"{c}:{InferDType(frame.Rows.Select(r => ForgeDataFrame.Get(r, c)))}").ToList();
    public static ForgeDataFrame DTypesFrame(this ForgeDataFrame frame) => new(frame.Columns.Select(c => Row("Column", c, "DType", InferDType(frame.Rows.Select(r => ForgeDataFrame.Get(r, c))))));
    public static ForgeDataFrame MemoryUsage(this ForgeDataFrame frame) => new(frame.Columns.Select(c => Row("Column", c, "Bytes", frame.Rows.Sum(r => EstimateBytes(ForgeDataFrame.Get(r, c))))));
    public static IReadOnlyList<int> Index(this ForgeDataFrame frame) => Enumerable.Range(0, frame.RowCount).ToList();

    public static object? At(this ForgeDataFrame frame, int row, string column) => row < 0 || row >= frame.RowCount ? null : ForgeDataFrame.Get(frame.Rows[row], column);
    public static object? IAt(this ForgeDataFrame frame, int row, int column)
    {
        var cols = frame.Columns; return row < 0 || row >= frame.RowCount || column < 0 || column >= cols.Count ? null : ForgeDataFrame.Get(frame.Rows[row], cols[column]);
    }

    public static ForgeDataFrame Filter(this ForgeDataFrame frame, params string[] likeOrColumns)
    {
        if (likeOrColumns.Length == 0) return frame;
        var cols = frame.Columns.Where(c => likeOrColumns.Any(p => c.Contains(p, StringComparison.OrdinalIgnoreCase) || c.Equals(p, StringComparison.OrdinalIgnoreCase))).ToArray();
        return frame.Select(cols);
    }

    public static ForgeDataFrame IsIn(this ForgeDataFrame frame, string column, IEnumerable<object?> values)
    {
        var set = values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return frame.Where(r => set.Contains(Convert.ToString(ForgeDataFrame.Get(r, column), CultureInfo.InvariantCulture)));
    }

    public static ForgeDataFrame WhereMask(this ForgeDataFrame frame, Func<IReadOnlyDictionary<string, object?>, bool> predicate, object? other = null)
        => new(frame.Rows.Select(r => predicate(r) ? Copy(r) : frame.Columns.ToDictionary(c => c, _ => other, StringComparer.OrdinalIgnoreCase)));

    public static ForgeDataFrame Mask(this ForgeDataFrame frame, Func<IReadOnlyDictionary<string, object?>, bool> predicate, object? other = null)
        => new(frame.Rows.Select(r => predicate(r) ? frame.Columns.ToDictionary(c => c, _ => other, StringComparer.OrdinalIgnoreCase) : Copy(r)));

    public static ForgeDataFrame BetweenTime(this ForgeDataFrame frame, string dateColumn, TimeSpan start, TimeSpan end)
        => frame.Where(r => TryDate(ForgeDataFrame.Get(r, dateColumn), out var d) && d.TimeOfDay >= start && d.TimeOfDay <= end);

    public static ForgeDataFrame Interpolate(this ForgeDataFrame frame, params string[] columns)
    {
        var cols = columns.Length == 0 ? frame.Columns.ToArray() : columns;
        var rows = frame.Rows.Select(Copy).ToList();
        foreach (var col in cols)
        {
            decimal? last = null;
            for (var i = 0; i < rows.Count; i++)
            {
                var v = ToDecimal(ForgeDataFrame.Get(rows[i], col));
                if (v.HasValue) { last = v; continue; }
                var next = NextDecimal(rows, col, i + 1);
                rows[i][col] = last.HasValue && next.HasValue ? (last.Value + next.Value) / 2 : last ?? next;
            }
        }
        return new ForgeDataFrame(rows);
    }

    public static ForgeDataFrame Replace(this ForgeDataFrame frame, object? oldValue, object? newValue)
        => new(frame.Rows.Select(r => frame.Columns.ToDictionary(c => c, c => Same(ForgeDataFrame.Get(r, c), oldValue) ? newValue : ForgeDataFrame.Get(r, c), StringComparer.OrdinalIgnoreCase)));

    public static ForgeDataFrame SortIndex(this ForgeDataFrame frame, bool ascending = true) => ascending ? frame : new ForgeDataFrame(frame.Rows.Reverse().Select(Copy));
    public static ForgeDataFrame RenameAxis(this ForgeDataFrame frame, string name) => frame.Assign("_axis", _ => name);
    public static ForgeDataFrame SetIndex(this ForgeDataFrame frame, string column, string indexName = "_index") => frame.Assign(indexName, r => ForgeDataFrame.Get(r, column));
    public static ForgeDataFrame ResetIndex(this ForgeDataFrame frame, string indexName = "index") => new(frame.Rows.Select((r, i) => { var c = Copy(r); c[indexName] = i; return c; }));
    public static ForgeDataFrame Reindex(this ForgeDataFrame frame, IEnumerable<int> indexes) => frame.Loc(indexes);
    public static ForgeDataFrame Transpose(this ForgeDataFrame frame)
    {
        var rows = new List<IDictionary<string, object?>>();
        for (var c = 0; c < frame.Columns.Count; c++)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Column"] = frame.Columns[c] };
            for (var i = 0; i < frame.RowCount; i++) row[$"Row{i}"] = ForgeDataFrame.Get(frame.Rows[i], frame.Columns[c]);
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }
    public static ForgeDataFrame T(this ForgeDataFrame frame) => frame.Transpose();

    public static ForgeDataFrame Aggregate(this ForgeDataFrame frame, params (string Column, string Function, string Alias)[] aggregations)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in aggregations) row[a.Alias] = AggregateValues(frame.Rows.Select(r => ForgeDataFrame.Get(r, a.Column)), a.Function);
        return new ForgeDataFrame(new[] { row });
    }
    public static ForgeDataFrame Agg(this ForgeDataFrame frame, params (string Column, string Function, string Alias)[] aggregations) => frame.Aggregate(aggregations);
    public static ForgeDataFrame Transform(this ForgeDataFrame frame, string column, string alias, Func<object?, object?> transform) => frame.Assign(alias, r => transform(ForgeDataFrame.Get(r, column)));
    public static ForgeDataFrame ApplyMap(this ForgeDataFrame frame, Func<object?, object?> map) => new(frame.Rows.Select(r => frame.Columns.ToDictionary(c => c, c => map(ForgeDataFrame.Get(r, c)), StringComparer.OrdinalIgnoreCase)));
    public static ForgeDataFrame MapColumn(this ForgeDataFrame frame, string column, string alias, Func<object?, object?> map) => frame.Transform(column, alias, map);

    public static decimal Sum(this ForgeDataFrame frame, string column) => Values(frame, column).Sum();
    public static decimal? Mean(this ForgeDataFrame frame, string column) { var v = Values(frame, column).ToArray(); return v.Length == 0 ? null : v.Average(); }
    public static decimal? Median(this ForgeDataFrame frame, string column) => Quantile(frame, column, 0.5m);
    public static object? Mode(this ForgeDataFrame frame, string column) => (frame.ValueCounts(column).Rows.FirstOrDefault() is { } modeRow && modeRow.TryGetValue("Value", out var mv) ? mv : null);
    public static object? MinValue(this ForgeDataFrame frame, string column) => frame.Rows.Select(r => ForgeDataFrame.Get(r, column)).Where(v => v is not null).OrderBy(v => v).FirstOrDefault();
    public static object? MaxValue(this ForgeDataFrame frame, string column) => frame.Rows.Select(r => ForgeDataFrame.Get(r, column)).Where(v => v is not null).OrderByDescending(v => v).FirstOrDefault();
    public static int Count(this ForgeDataFrame frame, string? column = null) => column is null ? frame.RowCount : frame.Rows.Count(r => ForgeDataFrame.Get(r, column) is not null and not DBNull);
    public static decimal? Std(this ForgeDataFrame frame, string column) => StdDev(Values(frame, column).ToArray());
    public static decimal? Var(this ForgeDataFrame frame, string column) { var v = Values(frame, column).ToArray(); var sd = StdDev(v); return sd is null ? null : sd * sd; }
    public static decimal? Sem(this ForgeDataFrame frame, string column) { var v = Values(frame, column).ToArray(); var sd = StdDev(v); return sd is null || v.Length == 0 ? null : sd / (decimal)Math.Sqrt(v.Length); }
    public static decimal Prod(this ForgeDataFrame frame, string column) { var p = 1m; foreach (var v in Values(frame, column)) p *= v; return p; }
    public static decimal? Quantile(this ForgeDataFrame frame, string column, decimal q)
    {
        var v = Values(frame, column).OrderBy(x => x).ToArray(); return v.Length == 0 ? null : Percentile(v, q);
    }
    public static int? IdxMin(this ForgeDataFrame frame, string column) => ExtremeIndex(frame, column, min: true);
    public static int? IdxMax(this ForgeDataFrame frame, string column) => ExtremeIndex(frame, column, min: false);

    public static ForgeDataFrame RollingMean(this ForgeDataFrame frame, string column, int window, string alias) => RollingCalc(frame, column, window, alias, v => v.Average());
    public static ForgeDataFrame RollingSum(this ForgeDataFrame frame, string column, int window, string alias) => RollingCalc(frame, column, window, alias, v => v.Sum());
    public static ForgeDataFrame RollingStd(this ForgeDataFrame frame, string column, int window, string alias) => RollingCalc(frame, column, window, alias, v => StdDev(v) ?? 0);
    public static ForgeDataFrame RollingVar(this ForgeDataFrame frame, string column, int window, string alias) => RollingCalc(frame, column, window, alias, v => { var sd = StdDev(v) ?? 0; return sd * sd; });
    public static ForgeDataFrame ExpandingMean(this ForgeDataFrame frame, string column, string alias) => RollingCalc(frame, column, frame.RowCount, alias, v => v.Average(), expanding: true);
    public static ForgeDataFrame ExpandingSum(this ForgeDataFrame frame, string column, string alias) => RollingCalc(frame, column, frame.RowCount, alias, v => v.Sum(), expanding: true);
    public static ForgeDataFrame EwmMean(this ForgeDataFrame frame, string column, string alias, decimal alpha = 0.5m)
    {
        var rows = frame.Rows.Select(Copy).ToList(); decimal? previous = null;
        for (var i = 0; i < rows.Count; i++) { var v = ToDecimal(ForgeDataFrame.Get(rows[i], column)); previous = v.HasValue ? previous.HasValue ? alpha * v.Value + (1 - alpha) * previous.Value : v.Value : previous; rows[i][alias] = previous; }
        return new ForgeDataFrame(rows);
    }

    public static ForgeDataFrame StrLower(this ForgeDataFrame frame, string column, string? alias = null) => frame.Transform(column, alias ?? column, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.ToLowerInvariant());
    public static ForgeDataFrame StrUpper(this ForgeDataFrame frame, string column, string? alias = null) => frame.Transform(column, alias ?? column, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.ToUpperInvariant());
    public static ForgeDataFrame StrTitle(this ForgeDataFrame frame, string column, string? alias = null) => frame.Transform(column, alias ?? column, v => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty));
    public static ForgeDataFrame StrStrip(this ForgeDataFrame frame, string column, string? alias = null) => frame.Transform(column, alias ?? column, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim());
    public static ForgeDataFrame StrReplace(this ForgeDataFrame frame, string column, string oldValue, string newValue, string? alias = null) => frame.Transform(column, alias ?? column, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.Replace(oldValue, newValue));
    public static ForgeDataFrame StrContains(this ForgeDataFrame frame, string column, string value, string alias = "Contains") => frame.Transform(column, alias, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.Contains(value, StringComparison.OrdinalIgnoreCase) == true);
    public static ForgeDataFrame StrStartsWith(this ForgeDataFrame frame, string column, string value, string alias = "StartsWith") => frame.Transform(column, alias, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.StartsWith(value, StringComparison.OrdinalIgnoreCase) == true);
    public static ForgeDataFrame StrEndsWith(this ForgeDataFrame frame, string column, string value, string alias = "EndsWith") => frame.Transform(column, alias, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.EndsWith(value, StringComparison.OrdinalIgnoreCase) == true);
    public static ForgeDataFrame StrLen(this ForgeDataFrame frame, string column, string alias = "Length") => frame.Transform(column, alias, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.Length ?? 0);
    public static ForgeDataFrame StrSplit(this ForgeDataFrame frame, string column, char separator = ',', string alias = "Parts") => frame.Transform(column, alias, v => (Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty).Split(separator));
    public static ForgeDataFrame StrExtract(this ForgeDataFrame frame, string column, string contains, string alias = "Extract") => frame.Transform(column, alias, v => { var s = Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty; return s.Contains(contains, StringComparison.OrdinalIgnoreCase) ? contains : null; });

    public static ForgeDataFrame DtYear(this ForgeDataFrame frame, string column, string alias = "Year") => DatePart(frame, column, alias, d => d.Year);
    public static ForgeDataFrame DtMonth(this ForgeDataFrame frame, string column, string alias = "Month") => DatePart(frame, column, alias, d => d.Month);
    public static ForgeDataFrame DtDay(this ForgeDataFrame frame, string column, string alias = "Day") => DatePart(frame, column, alias, d => d.Day);
    public static ForgeDataFrame DtHour(this ForgeDataFrame frame, string column, string alias = "Hour") => DatePart(frame, column, alias, d => d.Hour);
    public static ForgeDataFrame DtMinute(this ForgeDataFrame frame, string column, string alias = "Minute") => DatePart(frame, column, alias, d => d.Minute);
    public static ForgeDataFrame DtSecond(this ForgeDataFrame frame, string column, string alias = "Second") => DatePart(frame, column, alias, d => d.Second);
    public static ForgeDataFrame DtWeekday(this ForgeDataFrame frame, string column, string alias = "Weekday") => DatePart(frame, column, alias, d => (int)d.DayOfWeek);
    public static ForgeDataFrame DtDayName(this ForgeDataFrame frame, string column, string alias = "DayName") => DatePart(frame, column, alias, d => d.DayOfWeek.ToString());
    public static ForgeDataFrame DtMonthName(this ForgeDataFrame frame, string column, string alias = "MonthName") => DatePart(frame, column, alias, d => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(d.Month));

    public static ForgeDataFrame Abs(this ForgeDataFrame frame, string column, string? alias = null) => frame.Transform(column, alias ?? column, v => Math.Abs(ToDecimal(v) ?? 0));
    public static ForgeDataFrame Round(this ForgeDataFrame frame, string column, int decimals = 0, string? alias = null) => frame.Transform(column, alias ?? column, v => Math.Round(ToDecimal(v) ?? 0, decimals));
    public static ForgeDataFrame Clip(this ForgeDataFrame frame, string column, decimal lower, decimal upper, string? alias = null) => frame.Transform(column, alias ?? column, v => Math.Min(upper, Math.Max(lower, ToDecimal(v) ?? 0)));
    public static ForgeDataFrame CumSum(this ForgeDataFrame frame, string column, string alias = "CumSum") => Cum(frame, column, alias, (a, b) => a + b);
    public static ForgeDataFrame CumProd(this ForgeDataFrame frame, string column, string alias = "CumProd") => Cum(frame, column, alias, (a, b) => a * b, 1m);
    public static ForgeDataFrame CumMax(this ForgeDataFrame frame, string column, string alias = "CumMax") => Cum(frame, column, alias, Math.Max);
    public static ForgeDataFrame CumMin(this ForgeDataFrame frame, string column, string alias = "CumMin") => Cum(frame, column, alias, Math.Min);
    public static ForgeDataFrame Diff(this ForgeDataFrame frame, string column, string alias = "Diff") { decimal? prev = null; return frame.Assign(alias, r => { var v = ToDecimal(ForgeDataFrame.Get(r, column)); var d = v.HasValue && prev.HasValue ? v - prev : null; prev = v; return d; }); }
    public static ForgeDataFrame PctChange(this ForgeDataFrame frame, string column, string alias = "PctChange") { decimal? prev = null; return frame.Assign(alias, r => { var v = ToDecimal(ForgeDataFrame.Get(r, column)); var d = v.HasValue && prev.HasValue && prev != 0 ? (v - prev) / prev : null; prev = v; return d; }); }
    public static ForgeDataFrame RankValues(this ForgeDataFrame frame, string column, string alias = "Rank", bool ascending = true) => frame.Assign(alias, r => frame.Rows.Select(x => ForgeDataFrame.Get(x, column)).OrderBy(x => x).ToList().FindIndex(v => Same(v, ForgeDataFrame.Get(r, column))) + 1);

    public static decimal? Corr(this ForgeDataFrame frame, string x, string y) { var pairs = Pairs(frame, x, y); if (pairs.Count < 2) return null; var ax = pairs.Average(p => p.X); var ay = pairs.Average(p => p.Y); var num = pairs.Sum(p => (p.X - ax) * (p.Y - ay)); var den = (decimal)Math.Sqrt((double)(pairs.Sum(p => (p.X - ax) * (p.X - ax)) * pairs.Sum(p => (p.Y - ay) * (p.Y - ay)))); return den == 0 ? null : num / den; }
    public static decimal? Cov(this ForgeDataFrame frame, string x, string y) { var pairs = Pairs(frame, x, y); if (pairs.Count < 2) return null; var ax = pairs.Average(p => p.X); var ay = pairs.Average(p => p.Y); return pairs.Sum(p => (p.X - ax) * (p.Y - ay)) / (pairs.Count - 1); }
    public static decimal? Skew(this ForgeDataFrame frame, string column) { var v = Values(frame, column).ToArray(); if (v.Length < 3) return null; var mean = v.Average(); var sd = StdDev(v) ?? 0; return sd == 0 ? 0 : v.Sum(x => (decimal)Math.Pow((double)((x - mean) / sd), 3)) / v.Length; }
    public static decimal? Kurt(this ForgeDataFrame frame, string column) { var v = Values(frame, column).ToArray(); if (v.Length < 4) return null; var mean = v.Average(); var sd = StdDev(v) ?? 0; return sd == 0 ? 0 : v.Sum(x => (decimal)Math.Pow((double)((x - mean) / sd), 4)) / v.Length - 3; }
    public static decimal? Mad(this ForgeDataFrame frame, string column) { var v = Values(frame, column).ToArray(); if (v.Length == 0) return null; var mean = v.Average(); return v.Average(x => Math.Abs(x - mean)); }

    public static ForgeDataFrame Stack(this ForgeDataFrame frame) => frame.Melt([], frame.Columns.ToArray(), "Column", "Value");
    public static ForgeDataFrame Unstack(this ForgeDataFrame frame, string index, string columns, string values) => frame.PivotTable(index, columns, values, "first");
    public static ForgeDataFrame CrossTab(this ForgeDataFrame frame, string row, string column)
        => frame.PivotTable(row, column, column, "count");

    public static ForgeDataFrame Duplicated(this ForgeDataFrame frame, string alias = "Duplicated", params string[] columns)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cols = columns.Length == 0 ? frame.Columns.ToArray() : columns;
        return frame.Assign(alias, r => !seen.Add(string.Join("|", cols.Select(c => Convert.ToString(ForgeDataFrame.Get(r, c), CultureInfo.InvariantCulture)))));
    }

    public static ForgeDataFrame Astype(this ForgeDataFrame frame, string column, Type type, string? alias = null) => frame.Transform(column, alias ?? column, v => ChangeType(v, type));
    public static ForgeDataFrame ConvertDTypes(this ForgeDataFrame frame) => new(frame.Rows.Select(r => frame.Columns.ToDictionary(c => c, c => ForgePandas.Coerce(Convert.ToString(ForgeDataFrame.Get(r, c), CultureInfo.InvariantCulture)), StringComparer.OrdinalIgnoreCase)));
    public static ForgeDataFrame InferObjects(this ForgeDataFrame frame) => frame.ConvertDTypes();

    public static ForgeDataFrame Shift(this ForgeDataFrame frame, string column, int periods = 1, string alias = "Shift")
    {
        var rows = frame.Rows.Select(Copy).ToList();
        for (var i = 0; i < rows.Count; i++) rows[i][alias] = i - periods >= 0 && i - periods < rows.Count ? ForgeDataFrame.Get(rows[i - periods], column) : null;
        return new ForgeDataFrame(rows);
    }
    public static ForgeDataFrame AsFreq(this ForgeDataFrame frame, string dateColumn, TimeSpan frequency) => frame; // in-memory frames preserve rows; DB/time-index resampling belongs in query layer
    public static ForgeDataFrame Resample(this ForgeDataFrame frame, string dateColumn, string frequency, string valueColumn, string agg = "sum")
    {
        return frame.Assign("_period", r => PeriodKey(ForgeDataFrame.Get(r, dateColumn), frequency)).GroupBy("_period").Agg(new ForgeAggregation(valueColumn, AggFromName(agg), valueColumn + "_" + agg));
    }
    public static ForgeDataFrame TzLocalize(this ForgeDataFrame frame, string column, TimeSpan offset, string? alias = null) => frame.Transform(column, alias ?? column, v => TryDate(v, out var d) ? new DateTimeOffset(d.DateTime, offset) : null);
    public static ForgeDataFrame TzConvert(this ForgeDataFrame frame, string column, TimeSpan offset, string? alias = null) => frame.Transform(column, alias ?? column, v => TryDate(v, out var d) ? d.ToOffset(offset) : null);

    public static ForgeDataFrame ToCategory(this ForgeDataFrame frame, string column, string codesAlias = "Codes")
    {
        var f = ForgePandas.Factorize(frame.Rows.Select(r => ForgeDataFrame.Get(r, column)));
        var rows = frame.Rows.Select(Copy).ToList(); for (var i = 0; i < rows.Count; i++) rows[i][codesAlias] = f.Codes[i]; return new ForgeDataFrame(rows);
    }
    public static IReadOnlyList<object?> CatCategories(this ForgeDataFrame frame, string column) => frame.Unique(column);
    public static ForgeDataFrame CatCodes(this ForgeDataFrame frame, string column, string alias = "Codes") => frame.ToCategory(column, alias);
    public static ForgeDataFrame CatRenameCategories(this ForgeDataFrame frame, string column, IDictionary<object?, object?> map, string? alias = null) => frame.Transform(column, alias ?? column, v => map.FirstOrDefault(kv => Same(kv.Key, v)).Value ?? v);
    public static ForgeDataFrame CatAddCategories(this ForgeDataFrame frame, string column, params object?[] values) => frame; // categories are implicit in the column values
    public static ForgeDataFrame CatRemoveCategories(this ForgeDataFrame frame, string column, params object?[] values) => frame.Where(r => !values.Any(v => Same(v, ForgeDataFrame.Get(r, column))));

    public static ForgeDataFrame GetDummies(this ForgeDataFrame frame, string column, string prefix = "")
    {
        var categories = frame.Unique(column).Select(v => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "Null").ToArray();
        return new ForgeDataFrame(frame.Rows.Select(r => { var c = Copy(r); var value = Convert.ToString(ForgeDataFrame.Get(r, column), CultureInfo.InvariantCulture) ?? "Null"; foreach (var cat in categories) c[$"{prefix}{cat}"] = string.Equals(value, cat, StringComparison.OrdinalIgnoreCase) ? 1 : 0; return c; }));
    }
    public static ForgeDataFrame Factorize(this ForgeDataFrame frame, string column, string alias = "Code") => frame.ToCategory(column, alias);
    public static ForgeDataFrame Eval(this ForgeDataFrame frame, string alias, Func<IReadOnlyDictionary<string, object?>, object?> expression) => frame.Assign(alias, expression);

    public static ForgeDataFrame Pipe(this ForgeDataFrame frame, Func<ForgeDataFrame, ForgeDataFrame> pipe) => pipe(frame);
    public static ForgeDataFrame Combine(this ForgeDataFrame left, ForgeDataFrame right, Func<object?, object?, object?> combine)
    {
        var cols = left.Columns.Union(right.Columns, StringComparer.OrdinalIgnoreCase).ToArray(); var count = Math.Max(left.RowCount, right.RowCount);
        var rows = new List<IDictionary<string, object?>>();
        for (var i = 0; i < count; i++) rows.Add(cols.ToDictionary(c => c, c => combine(i < left.RowCount ? ForgeDataFrame.Get(left.Rows[i], c) : null, i < right.RowCount ? ForgeDataFrame.Get(right.Rows[i], c) : null), StringComparer.OrdinalIgnoreCase));
        return new ForgeDataFrame(rows);
    }
    public static ForgeDataFrame CombineFirst(this ForgeDataFrame left, ForgeDataFrame right) => left.Combine(right, (a, b) => a ?? b);
    public static bool All(this ForgeDataFrame frame, string column) => frame.Rows.All(r => ToBool(ForgeDataFrame.Get(r, column)) == true);
    public static bool Any(this ForgeDataFrame frame, string column) => frame.Rows.Any(r => ToBool(ForgeDataFrame.Get(r, column)) == true);
    public static bool Bool(this ForgeDataFrame frame) => frame.RowCount == 1 && frame.Columns.Count == 1 && ToBool(ForgeDataFrame.Get(frame.Rows[0], frame.Columns[0])) == true;

    public static ForgeDataFrame Copy(this ForgeDataFrame frame) => new(frame.Rows.Select(Copy));
    public static ForgeDataFrame Insert(this ForgeDataFrame frame, int position, string column, object? value)
    {
        var rows = new List<IDictionary<string, object?>>(); var oldCols = frame.Columns.ToList(); position = Math.Clamp(position, 0, oldCols.Count);
        foreach (var r in frame.Rows)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i <= oldCols.Count; i++) { if (i == position) row[column] = value; if (i < oldCols.Count) row[oldCols[i]] = ForgeDataFrame.Get(r, oldCols[i]); }
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }
    public static ForgeSeries Pop(this ForgeDataFrame frame, string column) => frame.Series(column);
    public static ForgeDataFrame Drop(this ForgeDataFrame frame, params string[] columns) => new(frame.Rows.Select(r => frame.Columns.Where(c => !columns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToDictionary(c => c, c => ForgeDataFrame.Get(r, c), StringComparer.OrdinalIgnoreCase)));
    public static bool EqualsFrame(this ForgeDataFrame left, ForgeDataFrame right) => left.RowCount == right.RowCount && left.Columns.SequenceEqual(right.Columns, StringComparer.OrdinalIgnoreCase) && !left.CompareFrame(right).Rows.Any();
    public static ForgeDataFrame CompareFrame(this ForgeDataFrame left, ForgeDataFrame right)
    {
        var rows = new List<IDictionary<string, object?>>(); var cols = left.Columns.Union(right.Columns, StringComparer.OrdinalIgnoreCase).ToArray(); var count = Math.Max(left.RowCount, right.RowCount);
        for (var i = 0; i < count; i++) foreach (var c in cols) { var a = i < left.RowCount ? ForgeDataFrame.Get(left.Rows[i], c) : null; var b = i < right.RowCount ? ForgeDataFrame.Get(right.Rows[i], c) : null; if (!Same(a, b)) rows.Add(Row("Row", i, "Column", c, "Left", a, "Right", b)); }
        return new ForgeDataFrame(rows);
    }
    public static ForgeDataFrame Explode(this ForgeDataFrame frame, string column)
    {
        var rows = new List<IDictionary<string, object?>>();
        foreach (var r in frame.Rows)
        {
            var value = ForgeDataFrame.Get(r, column);
            if (value is string || value is not IEnumerable enumerable) { rows.Add(Copy(r)); continue; }
            foreach (var item in enumerable) { var copy = Copy(r); copy[column] = item; rows.Add(copy); }
        }
        return new ForgeDataFrame(rows);
    }

    public static string ToJsonText(this ForgeDataFrame frame, bool indented = false) => JsonSerializer.Serialize(frame.Rows, new JsonSerializerOptions { WriteIndented = indented });
    public static void ToJson(this ForgeDataFrame frame, string path, bool indented = false) => File.WriteAllText(path, frame.ToJsonText(indented), Encoding.UTF8);
    public static string ToHtml(this ForgeDataFrame frame) => "<table><thead><tr>" + string.Concat(frame.Columns.Select(c => $"<th>{Esc(c)}</th>")) + "</tr></thead><tbody>" + string.Concat(frame.Rows.Select(r => "<tr>" + string.Concat(frame.Columns.Select(c => $"<td>{Esc(ForgeDataFrame.Get(r, c))}</td>")) + "</tr>")) + "</tbody></table>";
    public static string ToXml(this ForgeDataFrame frame, string root = "rows", string rowName = "row") => new XDocument(new XElement(root, frame.Rows.Select(r => new XElement(rowName, frame.Columns.Select(c => new XElement(c, ForgeDataFrame.Get(r, c))))))).ToString();
    public static string ToMarkdown(this ForgeDataFrame frame)
    {
        var cols = frame.Columns; var sb = new StringBuilder(); sb.AppendLine("| " + string.Join(" | ", cols) + " |"); sb.AppendLine("| " + string.Join(" | ", cols.Select(_ => "---")) + " |"); foreach (var r in frame.Rows) sb.AppendLine("| " + string.Join(" | ", cols.Select(c => Convert.ToString(ForgeDataFrame.Get(r, c), CultureInfo.InvariantCulture))) + " |"); return sb.ToString();
    }
    public static IReadOnlyList<IDictionary<string, object?>> ToDict(this ForgeDataFrame frame) => frame.ToDictionaries();
    public static object?[,] ToNumpy(this ForgeDataFrame frame) { var arr = new object?[frame.RowCount, frame.Columns.Count]; for (var i = 0; i < frame.RowCount; i++) for (var j = 0; j < frame.Columns.Count; j++) arr[i, j] = ForgeDataFrame.Get(frame.Rows[i], frame.Columns[j]); return arr; }

    public static void AssertFrameEqual(this ForgeDataFrame actual, ForgeDataFrame expected)
    {
        if (!actual.EqualsFrame(expected)) throw new InvalidOperationException("ForgeDataFrame assertion failed: frames are not equal. Differences: " + actual.CompareFrame(expected).ToJsonText());
    }

    private static Dictionary<string, object?> Copy(IReadOnlyDictionary<string, object?> row) => new(row, StringComparer.OrdinalIgnoreCase);
    private static IDictionary<string, object?> Row(params object?[] values) { var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase); for (var i = 0; i + 1 < values.Length; i += 2) d[Convert.ToString(values[i], CultureInfo.InvariantCulture) ?? $"Column{i}"] = values[i + 1]; return d; }
    private static bool Same(object? a, object? b) => string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    private static decimal? ToDecimal(object? value) => decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    private static IEnumerable<decimal> Values(ForgeDataFrame frame, string column) => frame.Rows.Select(r => ToDecimal(ForgeDataFrame.Get(r, column))).Where(v => v.HasValue).Select(v => v!.Value);
    private static decimal? StdDev(IReadOnlyList<decimal> values) { if (values.Count < 2) return null; var avg = values.Average(); return (decimal)Math.Sqrt((double)(values.Sum(v => (v - avg) * (v - avg)) / (values.Count - 1))); }
    private static decimal Percentile(IReadOnlyList<decimal> ordered, decimal p) { if (ordered.Count == 0) return 0; var pos = (ordered.Count - 1) * p; var lo = (int)Math.Floor(pos); var hi = (int)Math.Ceiling(pos); return lo == hi ? ordered[lo] : ordered[lo] + (ordered[hi] - ordered[lo]) * (pos - lo); }
    
    private static int EstimateBytes(object? value) => value switch { null or DBNull => 0, string s => Encoding.UTF8.GetByteCount(s), int or float => 4, long or double or decimal or DateTime or DateTimeOffset => 8, _ => Encoding.UTF8.GetByteCount(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty) };
    private static bool TryDate(object? value, out DateTimeOffset date) => DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    private static decimal? NextDecimal(IReadOnlyList<Dictionary<string, object?>> rows, string column, int start) { for (var i = start; i < rows.Count; i++) { var v = ToDecimal(ForgeDataFrame.Get(rows[i], column)); if (v.HasValue) return v; } return null; }
    private static object? AggregateValues(IEnumerable<object?> values, string function) => function.ToLowerInvariant() switch { "sum" => values.Select(ToDecimal).Where(x => x.HasValue).Sum(x => x!.Value), "mean" or "avg" => values.Select(ToDecimal).Where(x => x.HasValue).DefaultIfEmpty().Average(x => x ?? 0), "median" => Median(values), "min" => values.Where(v => v is not null).OrderBy(v => v).FirstOrDefault(), "max" => values.Where(v => v is not null).OrderByDescending(v => v).FirstOrDefault(), "count" or "size" => values.Count(v => v is not null and not DBNull), "prod" => values.Select(ToDecimal).Where(x => x.HasValue).Aggregate(1m, (a, b) => a * b!.Value), _ => values.FirstOrDefault(v => v is not null) };
    private static object? Median(IEnumerable<object?> values) { var v = values.Select(ToDecimal).Where(x => x.HasValue).Select(x => x!.Value).OrderBy(x => x).ToArray(); return v.Length == 0 ? null : Percentile(v, 0.5m); }
    private static int? ExtremeIndex(ForgeDataFrame frame, string column, bool min) { var bestIndex = -1; decimal? best = null; for (var i = 0; i < frame.RowCount; i++) { var v = ToDecimal(ForgeDataFrame.Get(frame.Rows[i], column)); if (!v.HasValue) continue; if (!best.HasValue || (min ? v < best : v > best)) { best = v; bestIndex = i; } } return bestIndex < 0 ? null : bestIndex; }
    private static ForgeDataFrame RollingCalc(ForgeDataFrame frame, string column, int window, string alias, Func<IReadOnlyList<decimal>, object?> calc, bool expanding = false) { var rows = frame.Rows.Select(Copy).ToList(); for (var i = 0; i < rows.Count; i++) { var start = expanding ? 0 : Math.Max(0, i - window + 1); var vals = rows.Skip(start).Take(i - start + 1).Select(r => ToDecimal(ForgeDataFrame.Get(r, column))).Where(v => v.HasValue).Select(v => v!.Value).ToArray(); rows[i][alias] = vals.Length == 0 ? null : calc(vals); } return new ForgeDataFrame(rows); }
    private static ForgeDataFrame DatePart(ForgeDataFrame frame, string column, string alias, Func<DateTimeOffset, object?> selector) => frame.Transform(column, alias, v => TryDate(v, out var d) ? selector(d) : null);
    private static ForgeDataFrame Cum(ForgeDataFrame frame, string column, string alias, Func<decimal, decimal, decimal> op, decimal? seed = null) { var acc = seed; return frame.Assign(alias, r => { var v = ToDecimal(ForgeDataFrame.Get(r, column)); if (!v.HasValue) return acc; acc = acc.HasValue ? op(acc.Value, v.Value) : v.Value; return acc; }); }
    private static IReadOnlyList<(decimal X, decimal Y)> Pairs(ForgeDataFrame frame, string x, string y) => frame.Rows.Select(r => (X: ToDecimal(ForgeDataFrame.Get(r, x)), Y: ToDecimal(ForgeDataFrame.Get(r, y)))).Where(p => p.X.HasValue && p.Y.HasValue).Select(p => (p.X!.Value, p.Y!.Value)).ToList();
    private static object? ChangeType(object? value, Type type) { if (value is null or DBNull) return null; if (type == typeof(string)) return Convert.ToString(value, CultureInfo.InvariantCulture); if (type == typeof(DateTimeOffset)) return TryDate(value, out var d) ? d : null; if (type.IsEnum) return Enum.Parse(type, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, true); return Convert.ChangeType(value, type, CultureInfo.InvariantCulture); }
    private static string PeriodKey(object? value, string frequency) { if (!TryDate(value, out var d)) return ""; return frequency.ToLowerInvariant() switch { "y" or "year" => d.ToString("yyyy", CultureInfo.InvariantCulture), "m" or "month" => d.ToString("yyyy-MM", CultureInfo.InvariantCulture), "d" or "day" => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "h" or "hour" => d.ToString("yyyy-MM-dd HH", CultureInfo.InvariantCulture), _ => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }; }
    private static ForgeAgg AggFromName(string name) => name.ToLowerInvariant() switch { "sum" => ForgeAgg.Sum(), "avg" or "mean" => ForgeAgg.Avg(), "min" => ForgeAgg.Min(), "max" => ForgeAgg.Max(), "median" => ForgeAgg.Median(), "count" or "size" => ForgeAgg.Count(), _ => ForgeAgg.Count() };
    private static bool? ToBool(object? value) { if (value is bool b) return b; if (bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed)) return parsed; if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var d)) return d != 0; return null; }
    private static string Esc(object? value) => System.Security.SecurityElement.Escape(Convert.ToString(value, CultureInfo.InvariantCulture)) ?? string.Empty;
}

public sealed record ForgeInterval(decimal Left, decimal Right, bool ClosedLeft = true, bool ClosedRight = false)
{
    public bool Contains(decimal value) => (ClosedLeft ? value >= Left : value > Left) && (ClosedRight ? value <= Right : value < Right);
}

public sealed record ForgeMultiIndex(IReadOnlyList<IReadOnlyList<object?>> Tuples)
{
    public static ForgeMultiIndex FromArrays(params IEnumerable<object?>[] arrays)
    {
        var materialized = arrays.Select(a => a.ToArray()).ToArray(); var count = materialized.Length == 0 ? 0 : materialized.Min(a => a.Length);
        return new ForgeMultiIndex(Enumerable.Range(0, count).Select(i => (IReadOnlyList<object?>)materialized.Select(a => a[i]).ToArray()).ToList());
    }
    public static ForgeMultiIndex FromTuples(params object?[][] tuples) => new(tuples.Select(t => (IReadOnlyList<object?>)t).ToList());
    public static ForgeMultiIndex FromProduct(params IEnumerable<object?>[] levels)
    {
        IEnumerable<IReadOnlyList<object?>> result = new[] { Array.Empty<object?>() };
        foreach (var level in levels) result = result.SelectMany(prefix => level.Select(v => (IReadOnlyList<object?>)prefix.Concat(new[] { v }).ToArray())).ToList();
        return new ForgeMultiIndex(result.ToList());
    }
}

public sealed record ForgeSparseDType(Type ValueType, object? FillValue = null);
public sealed record ForgeExcelFile(string Path);
public sealed record ForgeExcelWriter(string Path);
