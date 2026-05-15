namespace ForgeORM.DataFrame;

public sealed class ForgeGroupBy
{
    private readonly ForgeDataFrame _frame;
    private readonly string[] _keys;

    internal ForgeGroupBy(ForgeDataFrame frame, string[] keys)
    {
        _frame = frame;
        _keys = keys;
    }

    public ForgeDataFrame Agg(params ForgeAggregation[] aggregations)
    {
        var groups = _frame.Rows.GroupBy(row => string.Join("|", _keys.Select(k => ForgeDataFrame.Get(row, k)?.ToString() ?? string.Empty)), StringComparer.OrdinalIgnoreCase);
        var result = new List<IDictionary<string, object?>>();

        foreach (var group in groups)
        {
            var first = group.First();
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in _keys) row[key] = ForgeDataFrame.Get(first, key);
            foreach (var agg in aggregations)
            {
                var alias = string.IsNullOrWhiteSpace(agg.Alias) ? agg.Column + agg.Aggregate.Name : agg.Alias;
                row[alias] = agg.Column == "*" ? agg.Aggregate.Compute(group.Cast<object?>()) : agg.Aggregate.Compute(group.Select(r => ForgeDataFrame.Get(r, agg.Column)));
            }
            result.Add(row);
        }

        return new ForgeDataFrame(result);
    }
}

public sealed record ForgeAggregation(string Column, ForgeAgg Aggregate, string? Alias = null)
{
    public static ForgeAggregation Count(string column = "*", string? alias = null) => new(column, ForgeAgg.Count(), alias ?? "Count");
    public static ForgeAggregation Sum(string column, string? alias = null) => new(column, ForgeAgg.Sum(), alias);
    public static ForgeAggregation Avg(string column, string? alias = null) => new(column, ForgeAgg.Avg(), alias);
    public static ForgeAggregation Min(string column, string? alias = null) => new(column, ForgeAgg.Min(), alias);
    public static ForgeAggregation Max(string column, string? alias = null) => new(column, ForgeAgg.Max(), alias);
    public static ForgeAggregation Median(string column, string? alias = null) => new(column, ForgeAgg.Median(), alias);
    public static ForgeAggregation Percentile(string column, decimal percentile, string? alias = null) => new(column, ForgeAgg.Percentile(percentile), alias);
}
