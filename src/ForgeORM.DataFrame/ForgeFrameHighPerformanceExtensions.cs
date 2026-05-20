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

    public static ForgeVectorizedFrame<T> Vectorized<T>(this ForgeDataFrame frame) => new(frame);

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

    public ForgeVectorizedFrame Where(string column, ForgeVectorOperator op, object? value)
    {
        return new ForgeVectorizedFrame(_frame.Where(row => Compare(ForgeDataFrame.Get(row, column), op, value)));
    }

    public object? Aggregate(string column, ForgeAggregate aggregate)
    {
        return aggregate switch
        {
            ForgeAggregate.Count => Count(column),
            ForgeAggregate.Sum => Sum(column),
            ForgeAggregate.Average => Average(column),
            ForgeAggregate.Min => Min(column),
            ForgeAggregate.Max => Max(column),
            _ => null
        };
    }

    public decimal Sum(string column)
    {
        return _frame.Rows
            .Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)))
            .Where(x => x.HasValue)
            .Sum(x => x!.Value);
    }

    public decimal? Average(string column)
    {
        return Average(_frame.Rows.Select(r => ForgeDataFrame.Get(r, column)));
    }
    public object? Min(string column)
    {
        return _frame.Rows
            .Select(r => ForgeDataFrame.Get(r, column))
            .Where(x => x is IComparable)
            .Cast<IComparable>()
            .OrderBy(x => x)
            .FirstOrDefault();
    }

    public object? Max(string column)
    {
        return _frame.Rows
            .Select(r => ForgeDataFrame.Get(r, column))
            .Where(x => x is IComparable)
            .Cast<IComparable>()
            .OrderByDescending(x => x)
            .FirstOrDefault();
    }


    public int Count(string? column = null)
    {
        if (string.IsNullOrWhiteSpace(column))
            return _frame.Rows.Count;

        return _frame.Rows.Count(r => ForgeDataFrame.Get(r, column!) is not null and not DBNull);
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


public sealed class ForgeVectorizedFrame<T>
{
    private readonly ForgeDataFrame _frame;

    internal ForgeVectorizedFrame(ForgeDataFrame frame) => _frame = frame;

    public ForgeVectorizedFrame<T> Where(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var rows = _frame.Rows.Where(row => compiled(ToObject(row.ToDictionary()))).Select(row => new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase));
        return new ForgeVectorizedFrame<T>(new ForgeDataFrame(rows));
    }

    public decimal Sum(Expression<Func<T, decimal>> selector)
    {
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        return _frame.Rows
            .Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)))
            .Where(x => x.HasValue)
            .Sum(x => x!.Value);
    }

    public decimal Sum(Expression<Func<T, decimal?>> selector)
    {
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        return _frame.Rows
            .Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)))
            .Where(x => x.HasValue)
            .Sum(x => x!.Value);
    }

    public decimal? Average(Expression<Func<T, decimal>> selector)
    {
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        var values = _frame.Rows
            .Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();
        return values.Length == 0 ? null : values.Average();
    }

    public int Count(Expression<Func<T, object?>>? selector = null)
    {
        if (selector is null) return _frame.Rows.Count;
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        return _frame.Rows.Count(r => ForgeDataFrame.Get(r, column) is not null and not DBNull);
    }

    private static T ToObject(IDictionary<string, object?> row)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(row);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException($"Unable to create {typeof(T).Name} from ForgeDataFrame row.");
    }
}
