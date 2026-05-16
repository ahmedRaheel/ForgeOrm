using System.Linq.Expressions;

namespace ForgeORM.Analytics;

public sealed class ForgeWindowFunctionBuilder<T>
{
    private readonly ForgeWindowMetric<T> _metric;

    internal ForgeWindowFunctionBuilder(
        ForgeAnalyticsQuery<T> query,
        string function)
    {
        _metric = new ForgeWindowMetric<T>(query, function, null);
    }

    /// <summary>
    /// Executes the PartitionBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the PartitionBy operation.</returns>
    public ForgeWindowFunctionBuilder<T> PartitionBy(params Expression<Func<T, object?>>[] columns)
    {
        _metric.PartitionBy(columns);
        return this;
    }

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public ForgeWindowFunctionBuilder<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _metric.OrderBy(columns);
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    public ForgeWindowFunctionBuilder<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _metric.OrderByDescending(columns);
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenUnboundedPrecedingAndCurrentRow operation.
    /// </summary>
    /// <returns>The result of the RowsBetweenUnboundedPrecedingAndCurrentRow operation.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndCurrentRow()
    {
        _metric.RowsBetweenUnboundedPrecedingAndCurrentRow();
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenUnboundedPrecedingAndUnboundedFollowing operation.
    /// </summary>
    /// <returns>The result of the RowsBetweenUnboundedPrecedingAndUnboundedFollowing operation.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()
    {
        _metric.RowsBetweenUnboundedPrecedingAndUnboundedFollowing();
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenPrecedingAndCurrentRow operation.
    /// </summary>
    /// <param name="preceding">The preceding value.</param>
    /// <returns>The result of the RowsBetweenPrecedingAndCurrentRow operation.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenPrecedingAndCurrentRow(int preceding)
    {
        _metric.RowsBetweenPrecedingAndCurrentRow(preceding);
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenCurrentRowAndFollowing operation.
    /// </summary>
    /// <param name="following">The following value.</param>
    /// <returns>The result of the RowsBetweenCurrentRowAndFollowing operation.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenCurrentRowAndFollowing(int following)
    {
        _metric.RowsBetweenCurrentRowAndFollowing(following);
        return this;
    }

    /// <summary>
    /// Executes the As operation.
    /// </summary>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the As operation.</returns>
    public ForgeAnalyticsQuery<T> As(string alias) => _metric.As(alias);
}
