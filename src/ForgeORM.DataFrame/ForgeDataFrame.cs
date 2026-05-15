using System.Collections;
using System.Globalization;
using Microsoft.Data.Analysis;


namespace ForgeORM.DataFrame;

public sealed class ForgeDataFrame
{
    private readonly List<Dictionary<string, object?>> _rows;

    public ForgeDataFrame(IEnumerable<IDictionary<string, object?>> rows)
    {
        _rows = rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows => _rows;
    public IReadOnlyList<string> Columns => _rows.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    public int RowCount => _rows.Count;

    public static ForgeDataFrame Empty { get; } = new([]);

    public ForgeDataFrame Head(int count = 5) => new(_rows.Take(Math.Max(count, 0)));
    public ForgeDataFrame Tail(int count = 5) => new(_rows.Skip(Math.Max(0, _rows.Count - Math.Max(count, 0))));

    public ForgeDataFrame Select(params string[] columns)
        => new(_rows.Select(row => columns.ToDictionary(c => c, c => row.TryGetValue(c, out var v) ? v : null, StringComparer.OrdinalIgnoreCase)));

    public ForgeDataFrame Rename(string oldName, string newName)
    {
        foreach (var row in _rows)
        {
            if (!row.TryGetValue(oldName, out var value)) continue;
            row.Remove(oldName);
            row[newName] = value;
        }
        return this;
    }

    public ForgeDataFrame Where(Func<IReadOnlyDictionary<string, object?>, bool> predicate)
        => new(_rows.Where(r => predicate(r)));

    public ForgeDataFrame SortBy(string column, bool descending = false)
    {
        var sorted = descending
            ? _rows.OrderByDescending(x => x.TryGetValue(column, out var v) ? v : null, Comparer<object?>.Create(CompareValues))
            : _rows.OrderBy(x => x.TryGetValue(column, out var v) ? v : null, Comparer<object?>.Create(CompareValues));
        return new(sorted);
    }

    public ForgeDataFrame Assign(string column, Func<IReadOnlyDictionary<string, object?>, object?> valueFactory)
    {
        foreach (var row in _rows)
            row[column] = valueFactory(row);
        return this;
    }

    public ForgeDataFrame FillNa(object? value, params string[] columns)
    {
        var targetColumns = columns.Length == 0 ? Columns : columns;
        foreach (var row in _rows)
            foreach (var c in targetColumns)
                if (!row.TryGetValue(c, out var current) || current is null || current is DBNull)
                    row[c] = value;
        return this;
    }

    public ForgeDataFrame DropNa(params string[] columns)
    {
        var targetColumns = columns.Length == 0 ? Columns : columns;
        return new(_rows.Where(row => targetColumns.All(c => row.TryGetValue(c, out var v) && v is not null && v is not DBNull)));
    }

    public ForgeDataFrame DropDuplicates(params string[] columns)
    {
        var targetColumns = columns.Length == 0 ? Columns : columns;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return new(_rows.Where(row => seen.Add(string.Join("|", targetColumns.Select(c => row.TryGetValue(c, out var v) ? Convert.ToString(v, CultureInfo.InvariantCulture) : "")))));
    }

    public ForgeGroupBy GroupBy(params string[] columns) => new(this, columns);

    public ForgeDataFrame PivotTable(string rows, string columns, string values, ForgeAgg aggregate)
    {
        var columnKeys = _rows.Select(r => ToKey(Get(r, columns))).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var groups = _rows.GroupBy(r => ToKey(Get(r, rows)), StringComparer.OrdinalIgnoreCase);
        var result = new List<IDictionary<string, object?>>();

        foreach (var group in groups)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [rows] = group.Key };
            foreach (var col in columnKeys)
            {
                var slice = group.Where(r => string.Equals(ToKey(Get(r, columns)), col, StringComparison.OrdinalIgnoreCase));
                row[col] = aggregate.Compute(slice.Select(r => Get(r, values)));
            }
            result.Add(row);
        }

        return new(result);
    }

    public ForgeDataFrame Melt(string[] idVars, string[] valueVars, string variableName = "variable", string valueName = "value")
    {
        var result = new List<IDictionary<string, object?>>();
        foreach (var row in _rows)
        {
            foreach (var valueVar in valueVars)
            {
                var item = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in idVars) item[id] = Get(row, id);
                item[variableName] = valueVar;
                item[valueName] = Get(row, valueVar);
                result.Add(item);
            }
        }
        return new(result);
    }

    public ForgeDataFrame Merge(ForgeDataFrame right, string leftOn, string rightOn, ForgeJoinKind join = ForgeJoinKind.Inner)
    {
        var result = new List<IDictionary<string, object?>>();
        var rightLookup = right._rows.GroupBy(r => ToKey(Get(r, rightOn)), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var left in _rows)
        {
            var key = ToKey(Get(left, leftOn));
            if (rightLookup.TryGetValue(key, out var matches))
            {
                foreach (var match in matches)
                    result.Add(Combine(left, match, "right_"));
            }
            else if (join is ForgeJoinKind.Left or ForgeJoinKind.Full)
            {
                result.Add(new Dictionary<string, object?>(left, StringComparer.OrdinalIgnoreCase));
            }
        }

        if (join is ForgeJoinKind.Right or ForgeJoinKind.Full)
        {
            var leftKeys = _rows.Select(r => ToKey(Get(r, leftOn))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var rightRow in right._rows.Where(r => !leftKeys.Contains(ToKey(Get(r, rightOn)))))
                result.Add(new Dictionary<string, object?>(rightRow, StringComparer.OrdinalIgnoreCase));
        }

        return new(result);
    }

    public ForgeDataFrame Rolling(string valueColumn, int window, string outputColumn, ForgeAgg aggregate)
    {
        var result = _rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).ToList();
        for (var i = 0; i < result.Count; i++)
        {
            var slice = result.Skip(Math.Max(0, i - window + 1)).Take(Math.Min(window, i + 1)).Select(r => Get(r, valueColumn));
            result[i][outputColumn] = aggregate.Compute(slice);
        }
        return new(result);
    }

    public ForgeDataFrame Describe(params string[] numericColumns)
    {
        var cols = numericColumns.Length == 0 ? Columns.ToArray() : numericColumns;
        var result = new List<IDictionary<string, object?>>();
        foreach (var col in cols)
        {
            var values = _rows.Select(r => ToDecimal(Get(r, col))).Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();
            if (values.Count == 0) continue;
            result.Add(new Dictionary<string, object?>
            {
                ["Column"] = col,
                ["Count"] = values.Count,
                ["Mean"] = values.Average(),
                ["Min"] = values.Min(),
                ["P25"] = Percentile(values, 0.25m),
                ["Median"] = Percentile(values, 0.50m),
                ["P75"] = Percentile(values, 0.75m),
                ["Max"] = values.Max(),
                ["StdDev"] = StdDev(values)
            });
        }
        return new(result);
    }

    public Microsoft.Data.Analysis.DataFrame ToMicrosoftDataFrame()
    {
        var columns = new List<DataFrameColumn>();
        foreach (var col in Columns)
        {
            var values = _rows.Select(r => Get(r, col)).ToList();
            columns.Add(CreateColumn(col, values));
        }
        return new Microsoft.Data.Analysis.DataFrame(columns);
    }

    public static object? Get(IReadOnlyDictionary<string, object?> row, string column)
        => row.TryGetValue(column, out var value) ? value : null;

    private static IDictionary<string, object?> Combine(IDictionary<string, object?> left, IDictionary<string, object?> right, string rightPrefix)
    {
        var row = new Dictionary<string, object?>(left, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in right)
        {
            var key = row.ContainsKey(pair.Key) ? rightPrefix + pair.Key : pair.Key;
            row[key] = pair.Value;
        }
        return row;
    }

    private static string ToKey(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    internal static decimal? ToDecimal(object? value)
    {
        if (value is null or DBNull) return null;
        try { return Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    internal static decimal Percentile(IReadOnlyList<decimal> orderedValues, decimal percentile)
    {
        if (orderedValues.Count == 0) return 0;
        if (orderedValues.Count == 1) return orderedValues[0];
        var position = (orderedValues.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper) return orderedValues[lower];
        var weight = position - lower;
        return orderedValues[lower] + (orderedValues[upper] - orderedValues[lower]) * weight;
    }

    private static decimal StdDev(IReadOnlyList<decimal> values)
    {
        if (values.Count <= 1) return 0;
        var avg = values.Average();
        var variance = values.Sum(v => (v - avg) * (v - avg)) / (values.Count - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    private static int CompareValues(object? x, object? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        if (x is IComparable comparable) return comparable.CompareTo(y);
        return string.Compare(Convert.ToString(x, CultureInfo.InvariantCulture), Convert.ToString(y, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }



    private static DataFrameColumn CreateColumn(string name, IReadOnlyList<object?> values)
    {
        var nonNull = values.FirstOrDefault(v => v is not null and not DBNull);
        var type = nonNull?.GetType();

        if (type == typeof(int))
            return new Int32DataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (int?)null : Convert.ToInt32(v)));

        if (type == typeof(long))
            return new Int64DataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (long?)null : Convert.ToInt64(v)));

        if (type == typeof(float))
            return new SingleDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (float?)null : Convert.ToSingle(v)));

        if (type == typeof(double))
            return new DoubleDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (double?)null : Convert.ToDouble(v)));

        if (type == typeof(decimal))
            return new DoubleDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (double?)null : Convert.ToDouble(v)));

        if (type == typeof(bool))
            return new BooleanDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (bool?)null : Convert.ToBoolean(v)));

        return new StringDataFrameColumn(
            name,
            values.Select(v => v is null or DBNull ? null : v.ToString()));
    }
}

public enum ForgeJoinKind { Inner, Left, Right, Full }
