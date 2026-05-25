using System.Globalization;
using System.Text.Json;
using ForgeORM.DataFrame;

namespace ForgeORM.DataFrame.Enterprise;

/// <summary>
/// Enterprise analytics helpers for ForgeDataFrame.
/// </summary>
public static class ForgeEnterpriseDataFrameExtensions
{
    /// <summary>
    /// Filters rows using a dictionary predicate.
    /// </summary>
    public static ForgeDataFrame WhereRows(this ForgeDataFrame frame, Func<IReadOnlyDictionary<string, object?>, bool> predicate)
    {
        return new ForgeDataFrame(frame.Rows.Where(predicate).Select(x => new Dictionary<string, object?>(x, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Replaces null values in all rows.
    /// </summary>
    public static ForgeDataFrame FillNull(this ForgeDataFrame frame, object? value)
    {
        return new ForgeDataFrame(frame.Rows.Select(row => row.ToDictionary(x => x.Key, x => x.Value ?? value, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Sorts the frame by a column.
    /// </summary>
    public static ForgeDataFrame SortBy(this ForgeDataFrame frame, string column, bool descending = false)
    {
        var rows = descending
            ? frame.Rows.OrderByDescending(x => x.TryGetValue(column, out var v) ? v : null)
            : frame.Rows.OrderBy(x => x.TryGetValue(column, out var v) ? v : null);

        return new ForgeDataFrame(rows.Select(x => new Dictionary<string, object?>(x, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Groups rows and calculates one or more measures.
    /// </summary>
    public static ForgeDataFrame GroupByAggregate(this ForgeDataFrame frame, string groupColumn, params ForgeFrameMeasure[] measures)
    {
        var rows = frame.Rows.GroupBy(x => x.TryGetValue(groupColumn, out var value) ? value : null)
            .Select(group =>
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [groupColumn] = group.Key
                };

                foreach (var measure in measures)
                {
                    row[measure.Name] = Aggregate(group, measure);
                }

                return row;
            });

        return new ForgeDataFrame(rows);
    }

    /// <summary>
    /// Creates a pivot table from row, column and value fields.
    /// </summary>
    public static ForgeDataFrame Pivot(this ForgeDataFrame frame, string rowColumn, string columnColumn, string valueColumn, ForgeFrameAggregateKind aggregate = ForgeFrameAggregateKind.Sum)
    {
        var pivotColumns = frame.Rows.Select(x => x.TryGetValue(columnColumn, out var value) ? value?.ToString() ?? "Blank" : "Blank")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        var rows = frame.Rows.GroupBy(x => x.TryGetValue(rowColumn, out var value) ? value : null)
            .Select(group =>
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [rowColumn] = group.Key
                };

                foreach (var pivotColumn in pivotColumns)
                {
                    var subset = group.Where(x => string.Equals(x.TryGetValue(columnColumn, out var v) ? v?.ToString() : "Blank", pivotColumn, StringComparison.OrdinalIgnoreCase));
                    row[pivotColumn] = Aggregate(subset, new ForgeFrameMeasure(pivotColumn, valueColumn, aggregate));
                }

                return row;
            });

        return new ForgeDataFrame(rows);
    }

    /// <summary>
    /// Adds a rolling average column.
    /// </summary>
    public static ForgeDataFrame RollingAverage(this ForgeDataFrame frame, string sourceColumn, string targetColumn, int window)
    {
        window = Math.Max(1, window);
        var output = new List<IDictionary<string, object?>>();
        var values = frame.Rows.Select(x => ToDecimal(x.TryGetValue(sourceColumn, out var v) ? v : null)).ToArray();

        for (var i = 0; i < frame.Rows.Count; i++)
        {
            var row = new Dictionary<string, object?>(frame.Rows[i], StringComparer.OrdinalIgnoreCase);
            var start = Math.Max(0, i - window + 1);
            row[targetColumn] = values.Skip(start).Take(i - start + 1).Average();
            output.Add(row);
        }

        return new ForgeDataFrame(output);
    }

    /// <summary>
    /// Adds a dense rank column ordered by a numeric source column.
    /// </summary>
    public static ForgeDataFrame Rank(this ForgeDataFrame frame, string sourceColumn, string targetColumn, bool descending = true)
    {
        var ordered = descending
            ? frame.Rows.OrderByDescending(x => ToDecimal(x.TryGetValue(sourceColumn, out var v) ? v : null)).ToList()
            : frame.Rows.OrderBy(x => ToDecimal(x.TryGetValue(sourceColumn, out var v) ? v : null)).ToList();

        var output = new List<IDictionary<string, object?>>();
        var rank = 1;
        foreach (var row in ordered)
        {
            var copy = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase)
            {
                [targetColumn] = rank++
            };
            output.Add(copy);
        }

        return new ForgeDataFrame(output);
    }

    /// <summary>
    /// Exports frame rows to JSON text.
    /// </summary>
    public static string ToEnterpriseJson(this ForgeDataFrame frame)
    {
        return JsonSerializer.Serialize(frame.Rows, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object Aggregate(IEnumerable<IReadOnlyDictionary<string, object?>> rows, ForgeFrameMeasure measure)
    {
        var list = rows.ToArray();
        return measure.Kind switch
        {
            ForgeFrameAggregateKind.Count => list.Length,
            ForgeFrameAggregateKind.Sum => list.Sum(x => ToDecimal(x.TryGetValue(measure.Column, out var v) ? v : null)),
            ForgeFrameAggregateKind.Average => list.Length == 0 ? 0 : list.Average(x => ToDecimal(x.TryGetValue(measure.Column, out var v) ? v : null)),
            ForgeFrameAggregateKind.Min => list.Length == 0 ? 0 : list.Min(x => ToDecimal(x.TryGetValue(measure.Column, out var v) ? v : null)),
            ForgeFrameAggregateKind.Max => list.Length == 0 ? 0 : list.Max(x => ToDecimal(x.TryGetValue(measure.Column, out var v) ? v : null)),
            _ => 0
        };
    }

    private static decimal ToDecimal(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        if (value is decimal d)
        {
            return d;
        }

        if (value is IConvertible convertible && decimal.TryParse(convertible.ToString(CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0;
    }
}
