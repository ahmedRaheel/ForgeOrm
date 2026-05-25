using System.Linq.Expressions;
using System.Threading.Tasks;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

public static class ForgeFrameHighPerformanceExtensions
{
    public static ForgeFrameQuery<T> Parallel<T>(this ForgeFrameQuery<T> query)
    {
        query.EnableParallelExecution();
        return query;
    }

    public static ForgeFrameQuery<T> MaxDegreeOfParallelism<T>(this ForgeFrameQuery<T> query, int degree)
    {
        query.SetMaxDegreeOfParallelism(degree);
        return query;
    }

    public static ForgeVectorizedFrame Vectorized(this ForgeDataFrame frame) => new(frame);

    public static ForgeVectorizedFrame<T> Vectorized<T>(this ForgeDataFrame frame) => new(frame);

    public static ForgeDataFrame SortByDescending(this ForgeDataFrame frame, string column) => frame.SortBy(column, descending: true);

    public static ForgeDataFrame Take(this ForgeDataFrame frame, int count) => frame.Head(count);

    public static async ValueTask ExportCsvAsync(this ForgeDataFrame frame, string path, CancellationToken cancellationToken = default)
    {
        var columns = frame.Columns.ToArray();
        await using var stream = File.Create(path);
        await using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync(string.Join(",", columns.Select(EscapeCsv)));
        foreach (var row in frame.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = string.Join(",", columns.Select(c => EscapeCsv(Convert.ToString(ForgeDataFrame.Get(row, c), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)));
            await writer.WriteLineAsync(line);
        }
    }

    public static ForgeDataFrame Sum(this ForgeGroupBy group, string column, string? alias = null)
        => group.Agg(ForgeAggregation.Sum(column, alias ?? column));

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
