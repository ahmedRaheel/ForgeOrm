using ForgeORM.DataFrame;

namespace ForgeORM.Analytics;

public sealed class ForgeDistributedFramePlan
{
    /// <summary>
    /// Executes the RepartitionBy operation.
    /// </summary>
    /// <param name="Operations">The Operations value.</param>
    /// <returns>The result of the RepartitionBy operation.</returns>
    public string Name { get; init; } = "local";
    /// <summary>
    /// Executes the RepartitionBy operation.
    /// </summary>
    /// <param name="Operations">The Operations value.</param>
    /// <returns>The result of the RepartitionBy operation.</returns>
    public List<string> Partitions { get; } = [];
    /// <summary>
    /// Executes the RepartitionBy operation.
    /// </summary>
    /// <param name="Operations">The Operations value.</param>
    /// <returns>The result of the RepartitionBy operation.</returns>
    public List<string> Operations { get; } = [];
    /// <summary>
    /// Executes the RepartitionBy operation.
    /// </summary>
    /// <param name="Operations">The Operations value.</param>
    /// <returns>The result of the RepartitionBy operation.</returns>
    public ForgeDistributedFramePlan RepartitionBy(string column) { Operations.Add($"RepartitionBy:{column}"); return this; }
    /// <summary>
    /// Executes the Cache operation.
    /// </summary>
    /// <param name="Operations">The Operations value.</param>
    /// <returns>The result of the Cache operation.</returns>
    public ForgeDistributedFramePlan Cache() { Operations.Add("Cache"); return this; }
}

public static class ForgeAdvancedFrameExtensions
{
    /// <summary>
    /// Executes the ToDistributedPlan operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ToDistributedPlan operation.</returns>
    public static ForgeDistributedFramePlan ToDistributedPlan(this ForgeDataFrame frame, string name = "local")
        => new() { Name = name };

    /// <summary>
    /// Executes the WriteParquetAsync operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <param name="path">The path value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the WriteParquetAsync operation.</returns>
    public static ValueTask WriteParquetAsync(this ForgeDataFrame frame, string path, CancellationToken cancellationToken = default)
    {
        // Hook point for a future optional ForgeORM.Parquet package. Kept here as an adapter contract, not a hard dependency.
        File.WriteAllText(path + ".schema.txt", string.Join(Environment.NewLine, frame.Columns));
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes the AiInsights operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <returns>The result of the AiInsights operation.</returns>
    public static ForgeDataFrame AiInsights(this ForgeDataFrame frame)
        => frame.Describe(frame.Columns.ToArray());

    /// <summary>
    /// Executes the VectorizeText operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <param name="textColumn">The textColumn value.</param>
    /// <param name="vectorColumn">The vectorColumn value.</param>
    /// <returns>The result of the VectorizeText operation.</returns>
    public static ForgeDataFrame VectorizeText(this ForgeDataFrame frame, string textColumn, string vectorColumn)
        => frame.Assign(vectorColumn, r => $"vector-placeholder:{ForgeDataFrame.Get(r, textColumn)}");
}
