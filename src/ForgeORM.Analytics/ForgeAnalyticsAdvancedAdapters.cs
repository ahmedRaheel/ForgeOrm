using ForgeORM.DataFrame;

namespace ForgeORM.Analytics;

public sealed class ForgeDistributedFramePlan
{
    public string Name { get; init; } = "local";
    public List<string> Partitions { get; } = [];
    public List<string> Operations { get; } = [];
    public ForgeDistributedFramePlan RepartitionBy(string column) { Operations.Add($"RepartitionBy:{column}"); return this; }
    public ForgeDistributedFramePlan Cache() { Operations.Add("Cache"); return this; }
}

public static class ForgeAdvancedFrameExtensions
{
    public static ForgeDistributedFramePlan ToDistributedPlan(this ForgeDataFrame frame, string name = "local")
        => new() { Name = name };

    public static Task WriteParquetAsync(this ForgeDataFrame frame, string path, CancellationToken cancellationToken = default)
    {
        // Hook point for a future optional ForgeORM.Parquet package. Kept here as an adapter contract, not a hard dependency.
        File.WriteAllText(path + ".schema.txt", string.Join(Environment.NewLine, frame.Columns));
        return Task.CompletedTask;
    }

    public static ForgeDataFrame AiInsights(this ForgeDataFrame frame)
        => frame.Describe(frame.Columns.ToArray());

    public static ForgeDataFrame VectorizeText(this ForgeDataFrame frame, string textColumn, string vectorColumn)
        => frame.Assign(vectorColumn, r => $"vector-placeholder:{ForgeDataFrame.Get(r, textColumn)}");
}
