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
    /// Initializes or executes the PartitionBy operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public ForgeWindowFunctionBuilder<T> PartitionBy(params Expression<Func<T, object?>>[] columns)
    {
        _metric.PartitionBy(columns);
        return this;
    }

    /// <summary>
    /// Initializes or executes the OrderBy operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public ForgeWindowFunctionBuilder<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _metric.OrderBy(columns);
        return this;
    }

    /// <summary>
    /// Initializes or executes the OrderByDescending operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public ForgeWindowFunctionBuilder<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _metric.OrderByDescending(columns);
        return this;
    }

    /// <summary>
    /// Initializes or executes the RowsBetweenUnboundedPrecedingAndCurrentRow operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndCurrentRow()
    {
        _metric.RowsBetweenUnboundedPrecedingAndCurrentRow();
        return this;
    }

    /// <summary>
    /// Initializes or executes the RowsBetweenUnboundedPrecedingAndUnboundedFollowing operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()
    {
        _metric.RowsBetweenUnboundedPrecedingAndUnboundedFollowing();
        return this;
    }

    /// <summary>
    /// Initializes or executes the RowsBetweenPrecedingAndCurrentRow operation.
    /// </summary>
    /// <param name="preceding">The preceding value.</param>
    /// <returns>The operation result.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenPrecedingAndCurrentRow(int preceding)
    {
        _metric.RowsBetweenPrecedingAndCurrentRow(preceding);
        return this;
    }

    /// <summary>
    /// Initializes or executes the RowsBetweenCurrentRowAndFollowing operation.
    /// </summary>
    /// <param name="following">The following value.</param>
    /// <returns>The operation result.</returns>
    public ForgeWindowFunctionBuilder<T> RowsBetweenCurrentRowAndFollowing(int following)
    {
        _metric.RowsBetweenCurrentRowAndFollowing(following);
        return this;
    }

    /// <summary>
    /// Initializes or executes the As operation.
    /// </summary>
    /// <param name="alias">The alias value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAnalyticsQuery<T> As(string alias) => _metric.As(alias);
}
