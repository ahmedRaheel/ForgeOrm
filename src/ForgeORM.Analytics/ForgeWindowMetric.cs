using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Analytics;

public sealed class ForgeWindowMetric<T>
{
    private readonly ForgeAnalyticsQuery<T> _query;
    private readonly string _function;
    private readonly string? _suffix;
    private readonly List<string> _partitionBy = [];
    private readonly List<string> _orderBy = [];
    private string? _frame;

    internal ForgeWindowMetric(ForgeAnalyticsQuery<T> query, string function, string? suffix)
    {
        _query = query;
        _function = function;
        _suffix = suffix;
    }

    /// <summary>
    /// Executes the PartitionBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the PartitionBy operation.</returns>
    public ForgeWindowMetric<T> PartitionBy(params Expression<Func<T, object?>>[] columns)
    {
        _partitionBy.AddRange(columns.Select(ForgeAnalyticsQuery<T>.Column));
        return this;
    }

    /// <summary>
    /// Executes the PartitionBySql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the PartitionBySql operation.</returns>
    public ForgeWindowMetric<T> PartitionBySql(params string[] columns)
    {
        _partitionBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public ForgeWindowMetric<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => ForgeAnalyticsQuery<T>.Column(c) + " ASC"));
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    public ForgeWindowMetric<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => ForgeAnalyticsQuery<T>.Column(c) + " DESC"));
        return this;
    }

    /// <summary>
    /// Executes the OrderBySql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    public ForgeWindowMetric<T> OrderBySql(params string[] columns)
    {
        _orderBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    /// <summary>
    /// Executes the OverAll operation.
    /// </summary>
    /// <returns>The result of the OverAll operation.</returns>
    public ForgeWindowMetric<T> OverAll()
    {
        _partitionBy.Clear();
        _orderBy.Clear();
        _frame = null;
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenUnboundedPrecedingAndCurrentRow operation.
    /// </summary>
    /// <returns>The result of the RowsBetweenUnboundedPrecedingAndCurrentRow operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndCurrentRow()
    {
        _frame = "ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW";
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenUnboundedPrecedingAndUnboundedFollowing operation.
    /// </summary>
    /// <returns>The result of the RowsBetweenUnboundedPrecedingAndUnboundedFollowing operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()
    {
        _frame = "ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING";
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenPrecedingAndCurrentRow operation.
    /// </summary>
    /// <param name="preceding">The preceding value.</param>
    /// <returns>The result of the RowsBetweenPrecedingAndCurrentRow operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenPrecedingAndCurrentRow(int preceding)
    {
        _frame = $"ROWS BETWEEN {preceding} PRECEDING AND CURRENT ROW";
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenCurrentRowAndFollowing operation.
    /// </summary>
    /// <param name="following">The following value.</param>
    /// <returns>The result of the RowsBetweenCurrentRowAndFollowing operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenCurrentRowAndFollowing(int following)
    {
        _frame = $"ROWS BETWEEN CURRENT ROW AND {following} FOLLOWING";
        return this;
    }

    /// <summary>
    /// Executes the As operation.
    /// </summary>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the As operation.</returns>
    public ForgeAnalyticsQuery<T> As(string alias)
    {
        var over = new List<string>();

        if (_partitionBy.Count > 0)
            over.Add("PARTITION BY " + string.Join(", ", _partitionBy));

        if (_orderBy.Count > 0)
            over.Add("ORDER BY " + string.Join(", ", _orderBy));

        if (!string.IsNullOrWhiteSpace(_frame))
            over.Add(_frame);

        var sql = $"{_function} OVER ({string.Join(" ", over)}){_suffix} AS [{alias}]";
        return _query.AddSelect(sql);
    }
}
