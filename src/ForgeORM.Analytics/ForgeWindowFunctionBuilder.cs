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

    public ForgeWindowFunctionBuilder<T> PartitionBy(params Expression<Func<T, object?>>[] columns)
    {
        _metric.PartitionBy(columns);
        return this;
    }

    public ForgeWindowFunctionBuilder<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _metric.OrderBy(columns);
        return this;
    }

    public ForgeWindowFunctionBuilder<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _metric.OrderByDescending(columns);
        return this;
    }

    public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndCurrentRow()
    {
        _metric.RowsBetweenUnboundedPrecedingAndCurrentRow();
        return this;
    }

    public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()
    {
        _metric.RowsBetweenUnboundedPrecedingAndUnboundedFollowing();
        return this;
    }

    public ForgeWindowFunctionBuilder<T> RowsBetweenPrecedingAndCurrentRow(int preceding)
    {
        _metric.RowsBetweenPrecedingAndCurrentRow(preceding);
        return this;
    }

    public ForgeWindowFunctionBuilder<T> RowsBetweenCurrentRowAndFollowing(int following)
    {
        _metric.RowsBetweenCurrentRowAndFollowing(following);
        return this;
    }

    public ForgeAnalyticsQuery<T> As(string alias) => _metric.As(alias);
}
