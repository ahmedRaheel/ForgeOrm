using ForgeORM.DataFrame;

namespace ForgeORM.Analytics;

public sealed class ForgeDistributedFramePlan
{
    public string Name { get; init; } = "local";
    public List<string> Partitions { get; } = [];
    public List<string> Operations { get; } = [];
    /// <summary>
    /// Initializes or executes the RepartitionBy operation.
    /// </summary>
    /// <param name="Operations">The Operations value.</param>
    /// <returns>The operation result.</returns>
    public ForgeDistributedFramePlan RepartitionBy(string column) { Operations.Add($"RepartitionBy:{column}"); return this; }
    /// <summary>
    /// Initializes or executes the Cache operation.
    /// </summary>
    /// <param name="Operations">The Operations value.</param>
    /// <returns>The operation result.</returns>
    public ForgeDistributedFramePlan Cache() { Operations.Add("Cache"); return this; }
}

public static class ForgeAdvancedFrameExtensions
{
    /// <summary>
    /// Initializes or executes the ToDistributedPlan operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <param name="name">The name value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeDistributedFramePlan ToDistributedPlan(this ForgeDataFrame frame, string name = "local")
        => new() { Name = name };

    /// <summary>
    /// Initializes or executes the WriteParquetAsync operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <param name="path">The path value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static Task WriteParquetAsync(this ForgeDataFrame frame, string path, CancellationToken cancellationToken = default)
    {
        // Hook point for a future optional ForgeORM.Parquet package. Kept here as an adapter contract, not a hard dependency.
        File.WriteAllText(path + ".schema.txt", string.Join(Environment.NewLine, frame.Columns));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes or executes the AiInsights operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeDataFrame AiInsights(this ForgeDataFrame frame)
        => frame.Describe(frame.Columns.ToArray());

    /// <summary>
    /// Initializes or executes the VectorizeText operation.
    /// </summary>
    /// <param name="frame">The frame value.</param>
    /// <param name="textColumn">The textColumn value.</param>
    /// <param name="vectorColumn">The vectorColumn value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeDataFrame VectorizeText(this ForgeDataFrame frame, string textColumn, string vectorColumn)
        => frame.Assign(vectorColumn, r => $"vector-placeholder:{ForgeDataFrame.Get(r, textColumn)}");
}
