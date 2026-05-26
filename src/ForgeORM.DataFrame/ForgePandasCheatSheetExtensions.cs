using System.Globalization;
using System.Text.RegularExpressions;

namespace ForgeORM.DataFrame;

/// <summary>
/// Pandas-cheat-sheet inspired DataFrame API for ForgeORM analytics.
/// These helpers intentionally work over ForgeDataFrame so users can perform
/// selection, filtering, cleaning, reshaping, joins, window calculations,
/// string/date transformations and summary analytics without leaving ForgeORM.
/// </summary>
public static class ForgePandasCheatSheetExtensions
{
    /// <summary>Returns row and column counts, equivalent to pandas df.shape.</summary>
    public static ForgeFrameShape Shape(this ForgeDataFrame frame) => new(frame.RowCount, frame.Columns.Count);

    /// <summary>Returns a DataFrame containing column names and inferred CLR/data kinds.</summary>
    public static ForgeDataFrame DTypes(this ForgeDataFrame frame)
    {
        var rows = new List<IDictionary<string, object?>>();
        foreach (var column in frame.Columns)
        {
            rows.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Column"] = column,
                ["Kind"] = InferKind(frame, column),
                ["NonNull"] = frame.Rows.Count(r => !IsNull(ForgeDataFrame.Get(r, column))),
                ["Nulls"] = frame.Rows.Count(r => IsNull(ForgeDataFrame.Get(r, column)))
            });
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>Returns a compact pandas df.info()-style summary.</summary>
    public static ForgeFrameInfo Info(this ForgeDataFrame frame)
        => new(frame.RowCount, frame.Columns.Count, frame.Columns.ToArray());

    /// <summary>Returns a one-column DataFrame containing the column names.</summary>
    public static ForgeDataFrame ColumnsFrame(this ForgeDataFrame frame)
        => new ForgeDataFrame(frame.Columns.Select(c => new Dictionary<string, object?> { ["Column"] = c }));

    /// <summary>Returns the selected column as a one-column DataFrame.</summary>
    public static ForgeDataFrame Column(this ForgeDataFrame frame, string column)
        => frame.Select(column);

    /// <summary>Selects columns, equivalent to pandas df[[...]].</summary>
    public static ForgeDataFrame SelectColumns(this ForgeDataFrame frame, params string[] columns)
        => frame.Select(columns);

    /// <summary>Drops columns, equivalent to pandas df.drop(columns=[...]).</summary>
    public static ForgeDataFrame DropColumns(this ForgeDataFrame frame, params string[] columns)
    {
        var drop = new HashSet<string>(columns, StringComparer.OrdinalIgnoreCase);
        return new ForgeDataFrame(frame.Rows.Select(row => row
            .Where(kv => !drop.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>Renames columns using a map, equivalent to pandas df.rename(columns={...}).</summary>
    public static ForgeDataFrame RenameColumns(this ForgeDataFrame frame, IReadOnlyDictionary<string, string> map)
    {
        return new ForgeDataFrame(frame.Rows.Select(row =>
        {
            var output = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in row)
                output[map.TryGetValue(kv.Key, out var newName) ? newName : kv.Key] = kv.Value;
            return output;
        }));
    }

    /// <summary>Selects rows by zero-based position, equivalent to pandas df.iloc[start:end].</summary>
    public static ForgeDataFrame Iloc(this ForgeDataFrame frame, int start, int? end = null)
    {
        start = Math.Max(0, start);
        var take = Math.Max(0, (end ?? frame.RowCount) - start);
        return new ForgeDataFrame(frame.Rows.Skip(start).Take(take).Select(CloneRow));
    }

    /// <summary>Returns a value by row index and column name, equivalent to pandas df.at[index, column].</summary>
    public static object? At(this ForgeDataFrame frame, int rowIndex, string column)
        => rowIndex < 0 || rowIndex >= frame.RowCount ? null : ForgeDataFrame.Get(frame.Rows[rowIndex], column);

    /// <summary>Filters rows using a row predicate, equivalent to pandas boolean masks/query.</summary>
    public static ForgeDataFrame FilterRows(this ForgeDataFrame frame, Func<IReadOnlyDictionary<string, object?>, bool> predicate)
        => frame.Where(predicate);

    /// <summary>Filters rows where a column equals a value.</summary>
    public static ForgeDataFrame WhereEquals(this ForgeDataFrame frame, string column, object? value)
        => frame.Where(r => ValuesEqual(ForgeDataFrame.Get(r, column), value));

    /// <summary>Filters rows where a numeric column is between min and max.</summary>
    public static ForgeDataFrame Between(this ForgeDataFrame frame, string column, decimal min, decimal max, bool inclusive = true)
        => frame.Where(r =>
        {
            var value = ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column));
            return value.HasValue && (inclusive ? value.Value >= min && value.Value <= max : value.Value > min && value.Value < max);
        });

    /// <summary>Filters rows where column value is in the provided set, equivalent to pandas isin.</summary>
    public static ForgeDataFrame IsIn(this ForgeDataFrame frame, string column, params object?[] values)
    {
        var keys = new HashSet<string>(values.Select(ToKey), StringComparer.OrdinalIgnoreCase);
        return frame.Where(r => keys.Contains(ToKey(ForgeDataFrame.Get(r, column))));
    }

    /// <summary>Sorts by one or more columns, equivalent to pandas sort_values.</summary>
    public static ForgeDataFrame SortValues(this ForgeDataFrame frame, params string[] columns)
    {
        if (columns.Length == 0) return new ForgeDataFrame(frame.Rows.Select(CloneRow));
        var rows = frame.Rows.Select(CloneRow).ToList();
        rows.Sort((a, b) =>
        {
            foreach (var column in columns)
            {
                var descending = column.StartsWith("-", StringComparison.Ordinal);
                var name = descending ? column[1..] : column;
                var compare = Compare(ForgeDataFrame.Get(a, name), ForgeDataFrame.Get(b, name));
                if (compare != 0) return descending ? -compare : compare;
            }
            return 0;
        });
        return new ForgeDataFrame(rows);
    }

    /// <summary>Returns random rows. A seed can be supplied for deterministic tests.</summary>
    public static ForgeDataFrame Sample(this ForgeDataFrame frame, int count, int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        return new ForgeDataFrame(frame.Rows.OrderBy(_ => random.Next()).Take(Math.Max(0, count)).Select(CloneRow));
    }

    /// <summary>Returns the largest n rows by numeric column, equivalent to pandas nlargest.</summary>
    public static ForgeDataFrame NLargest(this ForgeDataFrame frame, int count, string column)
        => new ForgeDataFrame(frame.Rows.OrderByDescending(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)) ?? decimal.MinValue).Take(count).Select(CloneRow));

    /// <summary>Returns the smallest n rows by numeric column, equivalent to pandas nsmallest.</summary>
    public static ForgeDataFrame NSmallest(this ForgeDataFrame frame, int count, string column)
        => new ForgeDataFrame(frame.Rows.OrderBy(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)) ?? decimal.MaxValue).Take(count).Select(CloneRow));

    /// <summary>Adds/replaces a column from a row expression, equivalent to pandas assign.</summary>
    public static ForgeDataFrame WithColumn(this ForgeDataFrame frame, string column, Func<IReadOnlyDictionary<string, object?>, object?> expression)
        => frame.Assign(column, expression);

    /// <summary>Maps one column to another using a converter.</summary>
    public static ForgeDataFrame MapColumn(this ForgeDataFrame frame, string sourceColumn, string targetColumn, Func<object?, object?> converter)
        => frame.Assign(targetColumn, r => converter(ForgeDataFrame.Get(r, sourceColumn)));

    /// <summary>Replaces values in a column using a dictionary, equivalent to pandas replace/map.</summary>
    public static ForgeDataFrame ReplaceValues(this ForgeDataFrame frame, string column, IReadOnlyDictionary<object?, object?> replacements)
    {
        var replacementKeys = replacements.ToDictionary(k => ToKey(k.Key), v => v.Value, StringComparer.OrdinalIgnoreCase);
        return frame.MapColumn(column, column, value => replacementKeys.TryGetValue(ToKey(value), out var replacement) ? replacement : value);
    }

    /// <summary>Drops rows with null values in selected columns, equivalent to pandas dropna.</summary>
    public static ForgeDataFrame DropNulls(this ForgeDataFrame frame, params string[] columns)
        => frame.DropNa(columns);

    /// <summary>Fills null values in selected columns, equivalent to pandas fillna.</summary>
    public static ForgeDataFrame FillNulls(this ForgeDataFrame frame, object? value, params string[] columns)
        => frame.FillNa(value, columns);

    /// <summary>Returns a boolean mask DataFrame where values are null.</summary>
    public static ForgeDataFrame IsNullFrame(this ForgeDataFrame frame)
        => new ForgeDataFrame(frame.Rows.Select(row => frame.Columns.ToDictionary(c => c, c => (object?)IsNull(ForgeDataFrame.Get(row, c)), StringComparer.OrdinalIgnoreCase)));

    /// <summary>Returns a boolean mask DataFrame where values are not null.</summary>
    public static ForgeDataFrame NotNullFrame(this ForgeDataFrame frame)
        => new ForgeDataFrame(frame.Rows.Select(row => frame.Columns.ToDictionary(c => c, c => (object?)!IsNull(ForgeDataFrame.Get(row, c)), StringComparer.OrdinalIgnoreCase)));

    /// <summary>Returns unique values for a column, equivalent to pandas unique.</summary>
    public static IReadOnlyList<object?> Unique(this ForgeDataFrame frame, string column)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<object?>();
        foreach (var row in frame.Rows)
        {
            var value = ForgeDataFrame.Get(row, column);
            if (seen.Add(ToKey(value))) output.Add(value);
        }
        return output;
    }

    /// <summary>Returns distinct count for a column, equivalent to pandas nunique.</summary>
    public static int NUnique(this ForgeDataFrame frame, string column)
        => frame.NUnique(column);

    /// <summary>Returns value counts for a column, equivalent to pandas value_counts.</summary>
    public static ForgeDataFrame ValueCounts(this ForgeDataFrame frame, string column, string countColumn = "Count")
    {
        var groups = frame.Rows.GroupBy(r => ToKey(ForgeDataFrame.Get(r, column)), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => new Dictionary<string, object?>
            {
                [column] = g.FirstOrDefault() is { } first ? ForgeDataFrame.Get(first, column) : g.Key,
                [countColumn] = g.Count()
            });
        return new ForgeDataFrame(groups);
    }

    /// <summary>Counts non-null values by column, equivalent to pandas count.</summary>
    public static ForgeDataFrame CountNonNull(this ForgeDataFrame frame)
        => new ForgeDataFrame(frame.Columns.Select(c => new Dictionary<string, object?> { ["Column"] = c, ["Count"] = frame.Rows.Count(r => !IsNull(ForgeDataFrame.Get(r, c))) }));

    /// <summary>Casts a column to a requested kind, equivalent to pandas astype/to_numeric/to_datetime.</summary>
    public static ForgeDataFrame CastColumn(this ForgeDataFrame frame, string column, ForgeFrameColumnKind kind, string? targetColumn = null)
    {
        targetColumn ??= column;
        return frame.MapColumn(column, targetColumn, value => Cast(value, kind));
    }

    /// <summary>Converts a column to numeric decimal values.</summary>
    public static ForgeDataFrame ToNumeric(this ForgeDataFrame frame, string column, string? targetColumn = null)
        => frame.CastColumn(column, ForgeFrameColumnKind.Decimal, targetColumn);

    /// <summary>Converts a column to DateTime values.</summary>
    public static ForgeDataFrame ToDateTime(this ForgeDataFrame frame, string column, string? targetColumn = null)
        => frame.CastColumn(column, ForgeFrameColumnKind.DateTime, targetColumn);

    /// <summary>Filters rows whose string column contains the given text.</summary>
    public static ForgeDataFrame StringContains(this ForgeDataFrame frame, string column, string text, bool ignoreCase = true)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return frame.Where(r => Convert.ToString(ForgeDataFrame.Get(r, column), CultureInfo.InvariantCulture)?.Contains(text, comparison) == true);
    }

    /// <summary>Applies string lower-case transformation.</summary>
    public static ForgeDataFrame StringLower(this ForgeDataFrame frame, string column, string? targetColumn = null)
        => frame.MapColumn(column, targetColumn ?? column, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.ToLowerInvariant());

    /// <summary>Applies string upper-case transformation.</summary>
    public static ForgeDataFrame StringUpper(this ForgeDataFrame frame, string column, string? targetColumn = null)
        => frame.MapColumn(column, targetColumn ?? column, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.ToUpperInvariant());

    /// <summary>Trims string values.</summary>
    public static ForgeDataFrame StringTrim(this ForgeDataFrame frame, string column, string? targetColumn = null)
        => frame.MapColumn(column, targetColumn ?? column, v => Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim());

    /// <summary>Replaces text using string replacement or regex.</summary>
    public static ForgeDataFrame StringReplace(this ForgeDataFrame frame, string column, string oldValueOrPattern, string newValue, bool regex = false, string? targetColumn = null)
        => frame.MapColumn(column, targetColumn ?? column, v =>
        {
            var text = Convert.ToString(v, CultureInfo.InvariantCulture);
            if (text is null) return null;
            return regex ? Regex.Replace(text, oldValueOrPattern, newValue) : text.Replace(oldValueOrPattern, newValue, StringComparison.Ordinal);
        });

    /// <summary>Extracts DateTime year.</summary>
    public static ForgeDataFrame DateYear(this ForgeDataFrame frame, string column, string targetColumn)
        => frame.MapColumn(column, targetColumn, v => TryDate(v, out var d) ? d.Year : null);

    /// <summary>Extracts DateTime month.</summary>
    public static ForgeDataFrame DateMonth(this ForgeDataFrame frame, string column, string targetColumn)
        => frame.MapColumn(column, targetColumn, v => TryDate(v, out var d) ? d.Month : null);

    /// <summary>Extracts DateTime day.</summary>
    public static ForgeDataFrame DateDay(this ForgeDataFrame frame, string column, string targetColumn)
        => frame.MapColumn(column, targetColumn, v => TryDate(v, out var d) ? d.Day : null);

    /// <summary>Concatenates rows, equivalent to pandas concat axis=0.</summary>
    public static ForgeDataFrame ConcatRows(params ForgeDataFrame[] frames)
        => new ForgeDataFrame(frames.SelectMany(f => f.Rows.Select(CloneRow)));

    /// <summary>Concatenates columns by row position, equivalent to pandas concat axis=1.</summary>
    public static ForgeDataFrame ConcatColumns(ForgeDataFrame left, ForgeDataFrame right, string rightPrefix = "right_")
    {
        var rowCount = Math.Max(left.RowCount, right.RowCount);
        var rows = new List<IDictionary<string, object?>>();
        for (var i = 0; i < rowCount; i++)
        {
            var row = i < left.RowCount ? CloneRow(left.Rows[i]) : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (i < right.RowCount)
            {
                foreach (var kv in right.Rows[i])
                    row[row.ContainsKey(kv.Key) ? rightPrefix + kv.Key : kv.Key] = kv.Value;
            }
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>Merges two frames on a same-name key column.</summary>
    public static ForgeDataFrame MergeOn(this ForgeDataFrame left, ForgeDataFrame right, string on, ForgeJoinKind join = ForgeJoinKind.Inner)
        => left.Merge(right, on, on, join);

    /// <summary>Adds an index column, equivalent to reset_index for analytics output.</summary>
    public static ForgeDataFrame ResetIndex(this ForgeDataFrame frame, string indexColumn = "Index")
    {
        var rows = new List<IDictionary<string, object?>>();
        for (var i = 0; i < frame.RowCount; i++)
        {
            var row = CloneRow(frame.Rows[i]);
            row[indexColumn] = i;
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>Copies a column into an index-style column, equivalent to set_index while keeping row dictionaries simple.</summary>
    public static ForgeDataFrame SetIndex(this ForgeDataFrame frame, string column, string indexColumn = "Index")
        => frame.MapColumn(column, indexColumn, v => v);

    /// <summary>Adds shifted values, equivalent to pandas shift.</summary>
    public static ForgeDataFrame Shift(this ForgeDataFrame frame, string column, int periods = 1, string? targetColumn = null)
    {
        targetColumn ??= column + "_Shift";
        var rows = frame.Rows.Select(CloneRow).ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            var source = i - periods;
            rows[i][targetColumn] = source >= 0 && source < frame.RowCount ? ForgeDataFrame.Get(frame.Rows[source], column) : null;
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>Adds numeric difference from previous row, equivalent to pandas diff.</summary>
    public static ForgeDataFrame Diff(this ForgeDataFrame frame, string column, int periods = 1, string? targetColumn = null)
    {
        targetColumn ??= column + "_Diff";
        var rows = frame.Rows.Select(CloneRow).ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            var current = ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(frame.Rows[i], column));
            var source = i - periods;
            var previous = source >= 0 && source < frame.RowCount ? ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(frame.Rows[source], column)) : null;
            rows[i][targetColumn] = current.HasValue && previous.HasValue ? current.Value - previous.Value : null;
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>Adds cumulative sum, equivalent to pandas cumsum.</summary>
    public static ForgeDataFrame CumSum(this ForgeDataFrame frame, string column, string? targetColumn = null)
    {
        targetColumn ??= column + "_CumSum";
        decimal running = 0;
        return ApplyRunning(frame, targetColumn, r =>
        {
            var value = ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column));
            if (value.HasValue) running += value.Value;
            return running;
        });
    }

    /// <summary>Adds cumulative count, equivalent to pandas cumcount.</summary>
    public static ForgeDataFrame CumCount(this ForgeDataFrame frame, string targetColumn = "CumCount")
    {
        var i = 0;
        return ApplyRunning(frame, targetColumn, _ => i++);
    }

    /// <summary>Adds rolling sum over a numeric column.</summary>
    public static ForgeDataFrame RollingSum(this ForgeDataFrame frame, string column, int window, string? targetColumn = null)
        => frame.Rolling(column, window, targetColumn ?? column + "_RollingSum", ForgeAgg.Sum());

    /// <summary>Adds rolling average over a numeric column.</summary>
    public static ForgeDataFrame RollingMean(this ForgeDataFrame frame, string column, int window, string? targetColumn = null)
        => frame.Rolling(column, window, targetColumn ?? column + "_RollingMean", ForgeAgg.Avg());

    /// <summary>Adds expanding sum over a numeric column.</summary>
    public static ForgeDataFrame ExpandingSum(this ForgeDataFrame frame, string column, string? targetColumn = null)
        => frame.Rolling(column, Math.Max(1, frame.RowCount), targetColumn ?? column + "_ExpandingSum", ForgeAgg.Sum());

    /// <summary>Adds expanding average over a numeric column.</summary>
    public static ForgeDataFrame ExpandingMean(this ForgeDataFrame frame, string column, string? targetColumn = null)
        => frame.Rolling(column, Math.Max(1, frame.RowCount), targetColumn ?? column + "_ExpandingMean", ForgeAgg.Avg());

    /// <summary>Clips numeric values to lower/upper bounds, equivalent to pandas clip.</summary>
    public static ForgeDataFrame Clip(this ForgeDataFrame frame, string column, decimal lower, decimal upper, string? targetColumn = null)
        => frame.MapColumn(column, targetColumn ?? column, v =>
        {
            var value = ForgeDataFrame.ToDecimal(v);
            if (!value.HasValue) return null;
            if (value.Value < lower) return lower;
            if (value.Value > upper) return upper;
            return value.Value;
        });

    private static ForgeDataFrame ApplyRunning(ForgeDataFrame frame, string targetColumn, Func<IReadOnlyDictionary<string, object?>, object?> apply)
        => new ForgeDataFrame(frame.Rows.Select(row =>
        {
            var copy = CloneRow(row);
            copy[targetColumn] = apply(row);
            return copy;
        }));

    private static Dictionary<string, object?> CloneRow(IReadOnlyDictionary<string, object?> row)
        => new(row, StringComparer.OrdinalIgnoreCase);

    private static bool IsNull(object? value)
    {
        if (value is null or DBNull) return true;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase) || text.Equals("nan", StringComparison.OrdinalIgnoreCase) || text.Equals("n/a", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToKey(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private static bool ValuesEqual(object? left, object? right)
        => string.Equals(ToKey(left), ToKey(right), StringComparison.OrdinalIgnoreCase);

    private static int Compare(object? left, object? right)
    {
        if (left is null && right is null) return 0;
        if (left is null) return -1;
        if (right is null) return 1;
        var leftNumber = ForgeDataFrame.ToDecimal(left);
        var rightNumber = ForgeDataFrame.ToDecimal(right);
        if (leftNumber.HasValue && rightNumber.HasValue) return leftNumber.Value.CompareTo(rightNumber.Value);
        return string.Compare(ToKey(left), ToKey(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string InferKind(ForgeDataFrame frame, string column)
    {
        var values = frame.Rows.Select(r => ForgeDataFrame.Get(r, column)).Where(v => !IsNull(v)).Take(500).ToList();
        if (values.Count == 0) return "null";
        if (values.All(v => v is bool || bool.TryParse(ToKey(v), out _))) return "bool";
        if (values.All(v => long.TryParse(ToKey(v), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))) return "int";
        if (values.All(v => decimal.TryParse(ToKey(v), NumberStyles.Number, CultureInfo.InvariantCulture, out _))) return "decimal";
        if (values.All(v => DateTime.TryParse(ToKey(v), CultureInfo.InvariantCulture, DateTimeStyles.None, out _))) return "datetime";
        return "string";
    }

    private static object? Cast(object? value, ForgeFrameColumnKind kind)
    {
        if (IsNull(value)) return null;
        var text = ToKey(value);
        return kind switch
        {
            ForgeFrameColumnKind.String => text,
            ForgeFrameColumnKind.Int32 => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null,
            ForgeFrameColumnKind.Int64 => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : null,
            ForgeFrameColumnKind.Decimal => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null,
            ForgeFrameColumnKind.Double => double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f) ? f : null,
            ForgeFrameColumnKind.Boolean => bool.TryParse(text, out var b) ? b : text == "1" ? true : text == "0" ? false : null,
            ForgeFrameColumnKind.DateTime => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) ? dt : null,
            _ => value
        };
    }

    private static bool TryDate(object? value, out DateTime date)
    {
        if (value is DateTime dt) { date = dt; return true; }
        return DateTime.TryParse(ToKey(value), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    }
}

/// <summary>DataFrame shape similar to pandas df.shape.</summary>
public readonly record struct ForgeFrameShape(int Rows, int Columns);

/// <summary>DataFrame info summary similar to pandas df.info().</summary>
public sealed record ForgeFrameInfo(int Rows, int Columns, IReadOnlyList<string> ColumnNames);

/// <summary>Supported DataFrame cast target kinds.</summary>
public enum ForgeFrameColumnKind
{
    String,
    Int32,
    Int64,
    Decimal,
    Double,
    Boolean,
    DateTime
}
