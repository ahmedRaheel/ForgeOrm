using System.Linq.Expressions;
using System.Threading.Tasks;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

public enum ForgeVectorOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith
}

public enum ForgeAggregate
{
    Count,
    Sum,
    Average,
    Min,
    Max
}

public static class ForgeFrameHighPerformanceExtensions
{
    public static ForgeFrameQuery<T> Parallel<T>(this ForgeFrameQuery<T> query)
    {
        query.EnableParallelExecution();
        return query;
    }

    public static ForgeFrameQuery<T> MaxDegreeOfParallelism<T>(this ForgeFrameQuery<T> query, int degree)
    {
        query.SetMaxDegreeOfParallelism(degree);
        return query;
    }

    public static ForgeVectorizedFrame Vectorized(this ForgeDataFrame frame) => new(frame);

    public static ForgeDataFrame SortByDescending(this ForgeDataFrame frame, string column) => frame.SortBy(column, descending: true);

    public static ForgeDataFrame Take(this ForgeDataFrame frame, int count) => frame.Head(count);

    public static async Task ExportCsvAsync(this ForgeDataFrame frame, string path, CancellationToken cancellationToken = default)
    {
        var columns = frame.Columns.ToArray();
        await using var stream = File.Create(path);
        await using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync(string.Join(",", columns.Select(EscapeCsv)));
        foreach (var row in frame.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = string.Join(",", columns.Select(c => EscapeCsv(Convert.ToString(ForgeDataFrame.Get(row, c), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)));
            await writer.WriteLineAsync(line);
        }
    }

    public static ForgeDataFrame Sum(this ForgeGroupBy group, string column, string? alias = null)
        => group.Agg(ForgeAggregation.Sum(column, alias ?? column));

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}

public sealed class ForgeVectorizedFrame
{
    private readonly ForgeDataFrame _frame;

    internal ForgeVectorizedFrame(ForgeDataFrame frame) => _frame = frame;

    public ForgeDataFrame Where(string column, ForgeVectorOperator op, object? value)
    {
        return _frame.Where(row => Compare(ForgeDataFrame.Get(row, column), op, value));
    }

    public object? Aggregate(string column, ForgeAggregate aggregate)
    {
        var values = _frame.Rows.Select(r => ForgeDataFrame.Get(r, column)).ToArray();
        return aggregate switch
        {
            ForgeAggregate.Count => values.Count(v => v is not null and not DBNull),
            ForgeAggregate.Sum => values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Sum(x => x!.Value),
            ForgeAggregate.Average => Average(values),
            ForgeAggregate.Min => values.Where(x => x is IComparable).Cast<IComparable>().OrderBy(x => x).FirstOrDefault(),
            ForgeAggregate.Max => values.Where(x => x is IComparable).Cast<IComparable>().OrderByDescending(x => x).FirstOrDefault(),
            _ => null
        };
    }

    private static decimal? Average(IEnumerable<object?> values)
    {
        var list = values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        return list.Length == 0 ? null : list.Average();
    }

    private static bool Compare(object? current, ForgeVectorOperator op, object? expected)
    {
        if (op is ForgeVectorOperator.Contains or ForgeVectorOperator.StartsWith or ForgeVectorOperator.EndsWith)
        {
            var left = Convert.ToString(current, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var right = Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            return op switch
            {
                ForgeVectorOperator.Contains => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                ForgeVectorOperator.StartsWith => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
                ForgeVectorOperator.EndsWith => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        var cmp = CompareScalar(current, expected);
        return op switch
        {
            ForgeVectorOperator.Equal => cmp == 0,
            ForgeVectorOperator.NotEqual => cmp != 0,
            ForgeVectorOperator.GreaterThan => cmp > 0,
            ForgeVectorOperator.GreaterThanOrEqual => cmp >= 0,
            ForgeVectorOperator.LessThan => cmp < 0,
            ForgeVectorOperator.LessThanOrEqual => cmp <= 0,
            _ => false
        };
    }

    private static int CompareScalar(object? left, object? right)
    {
        if (left is null or DBNull) return right is null or DBNull ? 0 : -1;
        if (right is null or DBNull) return 1;
        var l = ForgeDataFrame.ToDecimal(left);
        var r = ForgeDataFrame.ToDecimal(right);
        if (l.HasValue && r.HasValue) return l.Value.CompareTo(r.Value);
        return string.Compare(Convert.ToString(left), Convert.ToString(right), StringComparison.OrdinalIgnoreCase);
    }
}

public static class ForgeVectorizedFrameAggregateExtensions
{
    public static decimal Sum(this ForgeDataFrame frame, string column)
    {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        return frame.Rows
            .Select(row => ForgeDataFrame.Get(row, column))
            .Select(ForgeDataFrame.ToDecimal)
            .Where(value => value.HasValue)
            .Sum(value => value!.Value);
    }

    public static decimal Average(this ForgeDataFrame frame, string column)
    {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        var values = frame.Rows
            .Select(row => ForgeDataFrame.Get(row, column))
            .Select(ForgeDataFrame.ToDecimal)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        return values.Length == 0 ? 0m : values.Average();
    }

    public static int Count(this ForgeDataFrame frame, string column)
    {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        return frame.Rows.Count(row => ForgeDataFrame.Get(row, column) is not null and not DBNull);
    }
}
