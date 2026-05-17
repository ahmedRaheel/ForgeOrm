using ForgeORM.DataFrame;

namespace ForgeORM.DataFrame.Enterprise;

/// <summary>
/// Builds report and pivot outputs from a ForgeDataFrame.
/// </summary>
public sealed class ForgeReportBuilder
{
    private readonly ForgeDataFrame _frame;
    private string? _row;
    private string? _column;
    private ForgeFrameMeasure? _measure;

    public ForgeReportBuilder(ForgeDataFrame frame)
    {
        _frame = frame;
    }

    /// <summary>
    /// Sets row dimension.
    /// </summary>
    public ForgeReportBuilder Dimension(string column)
    {
        _row = column;
        return this;
    }

    /// <summary>
    /// Sets pivot column dimension.
    /// </summary>
    public ForgeReportBuilder PivotBy(string column)
    {
        _column = column;
        return this;
    }

    /// <summary>
    /// Sets the report measure.
    /// </summary>
    public ForgeReportBuilder Measure(string name, string column, ForgeFrameAggregateKind aggregate)
    {
        _measure = new ForgeFrameMeasure(name, column, aggregate);
        return this;
    }

    /// <summary>
    /// Executes the report.
    /// </summary>
    public ForgeDataFrame Execute()
    {
        if (_row is null || _measure is null)
        {
            throw new InvalidOperationException("Report row dimension and measure are required.");
        }

        return _column is null
            ? _frame.GroupByAggregate(_row, _measure)
            : _frame.Pivot(_row, _column, _measure.Column, _measure.Kind);
    }
}
