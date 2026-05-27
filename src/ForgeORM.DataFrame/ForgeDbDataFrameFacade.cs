using System.Collections;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

/// <summary>
/// Single ForgeDb entry point for Pandas/DataFrame creation, import and analytics helpers.
/// Consumers should call db.Pandas(), db.DataFrame(...), db.ReadCsv(...), etc. instead of using ForgePandas directly.
/// </summary>
public static class ForgeDbDataFrameFacadeExtensions
{
    /// <summary>Returns the Pandas-style DataFrame facade bound to the current ForgeDb instance.</summary>
    public static ForgeDbPandasFacade Pandas(this ForgeDb db) => new(db);

    /// <summary>Alias for db.Pandas().</summary>
    public static ForgeDbPandasFacade DataFrame(this ForgeDb db) => new(db);

    /// <summary>Creates a dataframe from rows through the ForgeDb access point.</summary>
    public static ForgeDataFrame DataFrame(this ForgeDb db, IEnumerable rows) => new ForgeDbPandasFacade(db).DataFrame(rows);

    /// <summary>Creates a dataframe from column-oriented values through the ForgeDb access point.</summary>
    public static ForgeDataFrame DataFrame(this ForgeDb db, IDictionary<string, IEnumerable<object?>> columns) => new ForgeDbPandasFacade(db).DataFrame(columns);

    /// <summary>Creates a series through the ForgeDb access point.</summary>
    public static ForgeSeries Series(this ForgeDb db, IEnumerable<object?> values, string name = "Value") => new ForgeDbPandasFacade(db).Series(values, name);

    /// <summary>Reads a CSV file through the ForgeDb access point.</summary>
    public static ForgeDataFrame ReadCsv(this ForgeDb db, string path, bool hasHeader = true, char delimiter = ',') => new ForgeDbPandasFacade(db).ReadCsv(path, hasHeader, delimiter);

    /// <summary>Reads a CSV file asynchronously through the ForgeDb access point.</summary>
    public static ValueTask<ForgeDataFrame> ReadCsvAsync(this ForgeDb db, string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => new ForgeDbPandasFacade(db).ReadCsvAsync(path, hasHeader, delimiter, cancellationToken);

    /// <summary>Reads JSON through the ForgeDb access point.</summary>
    public static ForgeDataFrame ReadJson(this ForgeDb db, string path) => new ForgeDbPandasFacade(db).ReadJson(path);

    /// <summary>Reads JSON asynchronously through the ForgeDb access point.</summary>
    public static ValueTask<ForgeDataFrame> ReadJsonAsync(this ForgeDb db, string path, CancellationToken cancellationToken = default)
        => new ForgeDbPandasFacade(db).ReadJsonAsync(path, cancellationToken);

    /// <summary>Runs SQL and returns a dataframe through the ForgeDb access point.</summary>
    public static ValueTask<ForgeDataFrame> ReadSqlFrameAsync(this ForgeDb db, string sql, object? parameters = null, CancellationToken cancellationToken = default)
        => new ForgeDbPandasFacade(db).ReadSqlAsync(sql, parameters, cancellationToken);
}

/// <summary>
/// Pandas-style facade. This keeps all DataFrame creation/import utilities reachable from ForgeDb only,
/// while preserving chainable DataFrame instance methods for analytics operations.
/// </summary>
public sealed class ForgeDbPandasFacade
{
    private readonly ForgeDb _db;

    internal ForgeDbPandasFacade(ForgeDb db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public ForgeDataFrame DataFrame(IEnumerable rows) => ForgePandas.DataFrame(rows);

    public ForgeDataFrame DataFrame(IDictionary<string, IEnumerable<object?>> columns) => ForgePandas.DataFrame(columns);

    public ForgeSeries Series(IEnumerable<object?> values, string name = "Value") => ForgePandas.Series(values, name);

    public ForgeDataFrame FromDict(IDictionary<string, IEnumerable<object?>> columns) => ForgePandas.FromDict(columns);

    public ForgeDataFrame JsonNormalize(string json) => ForgePandas.JsonNormalize(json);

    public ForgeSeries DateRange(DateTime start, DateTime end, TimeSpan? frequency = null, string name = "Date") => ForgePandas.DateRange(start, end, frequency, name);

    public ForgeSeries PeriodRange(DateTime start, DateTime end, string frequency = "M", string name = "Period") => ForgePandas.PeriodRange(start, end, frequency, name);

    public ForgeSeries TimedeltaRange(TimeSpan start, TimeSpan end, TimeSpan? frequency = null, string name = "Timedelta") => ForgePandas.TimedeltaRange(start, end, frequency, name);

    public DateTimeOffset Timestamp(object? value) => ForgePandas.Timestamp(value);

    public TimeSpan Timedelta(object? value) => ForgePandas.Timedelta(value);

    public DateTimeOffset? ToDateTime(object? value) => ForgePandas.ToDateTime(value);

    public TimeSpan? ToTimedelta(object? value) => ForgePandas.ToTimedelta(value);

    public ForgeDataFrame ReadCsv(string path, bool hasHeader = true, char delimiter = ',') => ForgePandas.ReadCsv(path, hasHeader, delimiter);

    public ValueTask<ForgeDataFrame> ReadCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgePandas.ReadCsvAsync(path, hasHeader, delimiter, cancellationToken);

    public ForgeDataFrame ReadTable(string path, bool hasHeader = true, char delimiter = '\t') => ForgePandas.ReadTable(path, delimiter, hasHeader);

    public ForgeDataFrame ReadFwf(string path, params (string Name, int Start, int Length)[] columns) => ForgePandas.ReadFwf(path, columns);

    public ForgeDataFrame ReadExcel(string path, bool hasHeader = true, string? sheetName = null) => ForgePandas.ReadExcel(path, hasHeader, sheetName);

    public ForgeDataFrame ReadJson(string path) => ForgePandas.ReadJson(path);

    public ValueTask<ForgeDataFrame> ReadJsonAsync(string path, CancellationToken cancellationToken = default) => ForgePandas.ReadJsonAsync(path, cancellationToken);

    public ValueTask<ForgeDataFrame> ReadSqlAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        => ForgePandas.ReadSqlAsync(_db, sql, parameters, cancellationToken);

    public ForgeDataFrame ReadHtml(string html) => ForgePandas.ReadHtml(html);

    public ForgeDataFrame ReadXml(string xml) => ForgePandas.ReadXml(xml);

    public ForgeDataFrame ReadPickle(string path) => ForgePandas.ReadPickle(path);

    public IReadOnlyList<object?> Unique(IEnumerable<object?> values) => ForgePandas.Unique(values);

    public int NUnique(IEnumerable<object?> values) => ForgePandas.NUnique(values);

    public ForgeDataFrame ValueCounts(IEnumerable<object?> values, string valueColumn = "Value", string countColumn = "Count")
        => ForgePandas.ValueCounts(values, valueColumn, countColumn);

    public ForgeMultiIndex MultiIndexFromArrays(params IEnumerable<object?>[] arrays) => ForgePandas.MultiIndexFromArrays(arrays);

    public ForgeMultiIndex MultiIndexFromTuples(params object?[][] tuples) => ForgePandas.MultiIndexFromTuples(tuples);

    public ForgeMultiIndex MultiIndexFromProduct(params IEnumerable<object?>[] levels) => ForgePandas.MultiIndexFromProduct(levels);

    public ForgeInterval Interval(decimal left, decimal right, bool closedLeft = true, bool closedRight = false) => ForgePandas.Interval(left, right, closedLeft, closedRight);

    public IReadOnlyList<ForgeInterval> IntervalRange(decimal start, decimal end, decimal step) => ForgePandas.IntervalRange(start, end, step);

    public (IReadOnlyList<object?> Values, int[] Codes) Factorize(IEnumerable<object?> values) => ForgePandas.Factorize(values);

    public ForgeDataFrame GetDummies(IEnumerable<object?> values, string prefix = "")
    {
        var frame = ForgePandas.DataFrame(new Dictionary<string, IEnumerable<object?>> { ["Value"] = values });
        return frame.GetDummies("Value", prefix);
    }

    public ForgeDataFrame Concat(params ForgeDataFrame[] frames) => ForgePandasExtensions.Concat(frames);

    public ForgeDataFrame Merge(ForgeDataFrame left, ForgeDataFrame right, string on, ForgeJoinKind how = ForgeJoinKind.Inner) => left.Merge(right, on, how);

    public ForgeDataFrame Merge(ForgeDataFrame left, ForgeDataFrame right, string on, string how)
        => left.Merge(right, on, Enum.TryParse<ForgeJoinKind>(how, true, out var parsed) ? parsed : ForgeJoinKind.Inner);

    public ForgeDataFrame CrossTab(ForgeDataFrame frame, string rowColumn, string columnColumn) => frame.CrossTab(rowColumn, columnColumn);

    public ForgeExcelFile ExcelFile(string path) => new(path);

    public ForgeExcelWriter ExcelWriter(string path) => new(path);
}

internal static partial class ForgePandas
{
    public static int NUnique(IEnumerable<object?> values)
    {
        var count = 0;
        var seen = new List<object?>();
        foreach (var value in values)
        {
            if (seen.Any(x => string.Equals(Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture), Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)))
                continue;
            seen.Add(value);
            count++;
        }
        return count;
    }

    public static ForgeDataFrame ValueCounts(IEnumerable<object?> values, string valueColumn = "Value", string countColumn = "Count")
    {
        var rows = values
            .GroupBy(v => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => (IDictionary<string, object?>)new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                [valueColumn] = g.FirstOrDefault(),
                [countColumn] = g.Count()
            });

        return new ForgeDataFrame(rows);
    }
}
