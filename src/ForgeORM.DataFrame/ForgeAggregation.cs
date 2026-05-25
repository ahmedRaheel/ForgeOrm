namespace ForgeORM.DataFrame;

public sealed record ForgeAggregation(string Column, ForgeAgg Aggregate, string? Alias = null)
{
    /// <summary>
    /// Executes the Count operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Count operation.</returns>
    public static ForgeAggregation Count(string column = "*", string? alias = null) => new(column, ForgeAgg.Count(), alias ?? "Count");
    /// <summary>
    /// Executes the Sum operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Sum operation.</returns>
    public static ForgeAggregation Sum(string column, string? alias = null) => new(column, ForgeAgg.Sum(), alias);
    /// <summary>
    /// Executes the Avg operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Avg operation.</returns>
    public static ForgeAggregation Avg(string column, string? alias = null) => new(column, ForgeAgg.Avg(), alias);
    /// <summary>
    /// Executes the Min operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Min operation.</returns>
    public static ForgeAggregation Min(string column, string? alias = null) => new(column, ForgeAgg.Min(), alias);
    /// <summary>
    /// Executes the Max operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Max operation.</returns>
    public static ForgeAggregation Max(string column, string? alias = null) => new(column, ForgeAgg.Max(), alias);
    /// <summary>
    /// Executes the Median operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Median operation.</returns>
    public static ForgeAggregation Median(string column, string? alias = null) => new(column, ForgeAgg.Median(), alias);
    /// <summary>
    /// Executes the Percentile operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="percentile">The percentile value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Percentile operation.</returns>
    public static ForgeAggregation Percentile(string column, decimal percentile, string? alias = null) => new(column, ForgeAgg.Percentile(percentile), alias);
}
