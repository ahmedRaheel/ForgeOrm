using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

public static class ForgeFrameExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeFrameQuery<T> Frame<T>(this ForgeDb db) => new(db);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeFrameQuery<T> Frame<T>(this ForgeDbContext db) => new(db);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeDataFrame ToForgeFrame<T>(this IEnumerable<T> rows)
        => new(rows.Select(ToDictionary));

    /// <summary>
    /// Executes the ReadCsv operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <returns>The result of the ReadCsv operation.</returns>
    public static ForgeDataFrame ReadCsv(string path, bool hasHeader = true, char delimiter = ',')
        => ForgeDataFrame.FromCsv(path, hasHeader, delimiter);

    /// <summary>
    /// Executes the ReadCsvAsync operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadCsvAsync operation.</returns>
    public static ValueTask<ForgeDataFrame> ReadCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(path, hasHeader, delimiter, cancellationToken);

    /// <summary>
    /// Executes the ReadCsvAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadCsvAsync operation.</returns>
    public static ValueTask<ForgeDataFrame> ReadCsvAsync(Stream stream, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(stream, hasHeader, delimiter, cancellationToken);

    /// <summary>
    /// Executes the ReadJson operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <returns>The result of the ReadJson operation.</returns>
    public static ForgeDataFrame ReadJson(string path)
        => ForgeDataFrame.FromJson(path);

    /// <summary>
    /// Executes the ReadJsonAsync operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadJsonAsync operation.</returns>
    public static ValueTask<ForgeDataFrame> ReadJsonAsync(string path, CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromJsonAsync(path, cancellationToken);

    /// <summary>
    /// Executes the ReadJsonAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadJsonAsync operation.</returns>
    public static ValueTask<ForgeDataFrame> ReadJsonAsync(Stream stream, CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromJsonAsync(stream, cancellationToken);

    private static IDictionary<string, object?> ToDictionary<T>(T row)
    {
        if (row is IDictionary<string, object?> dict) return new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);
        if (row is IDictionary nonGeneric)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry item in nonGeneric) result[item.Key.ToString() ?? string.Empty] = item.Value;
            return result;
        }

        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p.GetValue(row), StringComparer.OrdinalIgnoreCase);
    }
}
