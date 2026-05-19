using System.Linq.Expressions;
using System.Globalization;
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
    private int _maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount);

    internal ForgeVectorizedFrame(ForgeDataFrame frame) => _frame = frame;

    public ForgeVectorizedFrame Where(string column, ForgeVectorOperator op, object? value)
        => new(_frame.Where(row => Compare(ForgeDataFrame.Get(row, column), op, value)))
        {
            _maxDegreeOfParallelism = _maxDegreeOfParallelism
        };

    public ForgeVectorizedFrame MaxDegreeOfParallelism(int degree)
    {
        if (degree <= 0) throw new ArgumentOutOfRangeException(nameof(degree));
        _maxDegreeOfParallelism = degree;
        return this;
    }

    public ForgeDataFrame ToFrame() => _frame;

    public int Count(string? column = null)
    {
        if (string.IsNullOrWhiteSpace(column)) return _frame.Rows.Count;
        return _frame.Rows.Count(r => ForgeDataFrame.Get(r, column!) is not null and not DBNull);
    }

    public decimal Sum(string column) => NumericValues(column).Sum();

    public decimal Average(string column)
    {
        var values = NumericValues(column).ToArray();
        return values.Length == 0 ? 0m : values.Average();
    }

    public object? Min(string column) => ComparableValues(column).OrderBy(x => x).FirstOrDefault();

    public object? Max(string column) => ComparableValues(column).OrderByDescending(x => x).FirstOrDefault();

    public object? Aggregate(string column, ForgeAggregate aggregate)
        => aggregate switch
        {
            ForgeAggregate.Count => Count(column),
            ForgeAggregate.Sum => Sum(column),
            ForgeAggregate.Average => Average(column),
            ForgeAggregate.Min => Min(column),
            ForgeAggregate.Max => Max(column),
            _ => null
        };

    private IEnumerable<decimal> NumericValues(string column)
        => _frame.Rows.Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column))).Where(x => x.HasValue).Select(x => x!.Value);

    private IEnumerable<IComparable> ComparableValues(string column)
        => _frame.Rows.Select(r => ForgeDataFrame.Get(r, column)).Where(x => x is IComparable).Cast<IComparable>();

    internal static bool Compare(object? current, ForgeVectorOperator op, object? expected)
    {
        if (op is ForgeVectorOperator.Contains or ForgeVectorOperator.StartsWith or ForgeVectorOperator.EndsWith)
        {
            var left = Convert.ToString(current, CultureInfo.InvariantCulture) ?? string.Empty;
            var right = Convert.ToString(expected, CultureInfo.InvariantCulture) ?? string.Empty;
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
        return string.Compare(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ForgeVectorizedFrame<T>
{
    private readonly ForgeDataFrame _frame;
    private int _maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount);

    internal ForgeVectorizedFrame(ForgeDataFrame frame) => _frame = frame;

    public ForgeVectorizedFrame<T> Where(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var compiled = predicate.Compile();
        var rows = _frame.Rows.Where(row => compiled(Materialize(row))).Select(row => new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase));
        return new ForgeVectorizedFrame<T>(new ForgeDataFrame(rows)) { _maxDegreeOfParallelism = _maxDegreeOfParallelism };
    }

    public ForgeVectorizedFrame<T> Where(string column, ForgeVectorOperator op, object? value)
        => new(new ForgeVectorizedFrame(_frame).Where(column, op, value).ToFrame()) { _maxDegreeOfParallelism = _maxDegreeOfParallelism };

    public ForgeVectorizedFrame<T> MaxDegreeOfParallelism(int degree)
    {
        if (degree <= 0) throw new ArgumentOutOfRangeException(nameof(degree));
        _maxDegreeOfParallelism = degree;
        return this;
    }

    public ForgeDataFrame ToFrame() => _frame;

    public decimal Sum(Expression<Func<T, decimal>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var compiled = selector.Compile();
        return _frame.Rows
            .AsParallel()
            .WithDegreeOfParallelism(_maxDegreeOfParallelism)
            .Select(row => compiled(Materialize(row)))
            .Sum();
    }

    public decimal Average(Expression<Func<T, decimal>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var compiled = selector.Compile();
        return _frame.Rows
            .AsParallel()
            .WithDegreeOfParallelism(_maxDegreeOfParallelism)
            .Select(row => compiled(Materialize(row)))
            .DefaultIfEmpty(0m)
            .Average();
    }

    public decimal Min(Expression<Func<T, decimal>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var compiled = selector.Compile();
        return _frame.Rows
            .AsParallel()
            .WithDegreeOfParallelism(_maxDegreeOfParallelism)
            .Select(row => compiled(Materialize(row)))
            .DefaultIfEmpty(0m)
            .Min();
    }

    public decimal Max(Expression<Func<T, decimal>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var compiled = selector.Compile();
        return _frame.Rows
            .AsParallel()
            .WithDegreeOfParallelism(_maxDegreeOfParallelism)
            .Select(row => compiled(Materialize(row)))
            .DefaultIfEmpty(0m)
            .Max();
    }

    public decimal Sum(string column) => new ForgeVectorizedFrame(_frame).MaxDegreeOfParallelism(_maxDegreeOfParallelism).Sum(column);
    public decimal Average(string column) => new ForgeVectorizedFrame(_frame).MaxDegreeOfParallelism(_maxDegreeOfParallelism).Average(column);
    public object? Min(string column) => new ForgeVectorizedFrame(_frame).MaxDegreeOfParallelism(_maxDegreeOfParallelism).Min(column);
    public object? Max(string column) => new ForgeVectorizedFrame(_frame).MaxDegreeOfParallelism(_maxDegreeOfParallelism).Max(column);

    private static T Materialize(IReadOnlyDictionary<string, object?> row)
    {
        var obj = Activator.CreateInstance<T>();
        foreach (var property in typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!property.CanWrite) continue;
            if (!row.TryGetValue(property.Name, out var value) || value is null or DBNull) continue;
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (targetType.IsEnum)
            {
                property.SetValue(obj, value is string s ? Enum.Parse(targetType, s, ignoreCase: true) : Enum.ToObject(targetType, value));
                continue;
            }
            property.SetValue(obj, Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));
        }
        return obj;
    }
}
