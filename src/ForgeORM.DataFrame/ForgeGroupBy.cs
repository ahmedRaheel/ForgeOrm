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

    /// <summary>
    /// Initializes or executes the Agg operation.
    /// </summary>
    /// <param name="aggregations">The aggregations value.</param>
    /// <returns>The operation result.</returns>
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
    /// <summary>
    /// Initializes or executes the Count operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAggregation Count(string column = "*", string? alias = null) => new(column, ForgeAgg.Count(), alias ?? "Count");
    /// <summary>
    /// Initializes or executes the Sum operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAggregation Sum(string column, string? alias = null) => new(column, ForgeAgg.Sum(), alias);
    /// <summary>
    /// Initializes or executes the Avg operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAggregation Avg(string column, string? alias = null) => new(column, ForgeAgg.Avg(), alias);
    /// <summary>
    /// Initializes or executes the Min operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAggregation Min(string column, string? alias = null) => new(column, ForgeAgg.Min(), alias);
    /// <summary>
    /// Initializes or executes the Max operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAggregation Max(string column, string? alias = null) => new(column, ForgeAgg.Max(), alias);
    /// <summary>
    /// Initializes or executes the Median operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAggregation Median(string column, string? alias = null) => new(column, ForgeAgg.Median(), alias);
    /// <summary>
    /// Initializes or executes the Percentile operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="percentile">The percentile value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeAggregation Percentile(string column, decimal percentile, string? alias = null) => new(column, ForgeAgg.Percentile(percentile), alias);
}
