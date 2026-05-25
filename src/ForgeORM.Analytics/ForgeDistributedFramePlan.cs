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
