using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeORM.DataFrame;

/// <summary>
/// Pandas-style factory helpers for creating <see cref="ForgeDataFrame"/> and <see cref="ForgeSeries"/> instances.
/// These helpers intentionally avoid heavy runtime dependencies and keep the API friendly for everyday analytics.
/// </summary>
public static class ForgePandas
{
    /// <summary>
    /// Creates a dataframe from dictionaries, objects, records, tuples or nested enumerable values.
    /// </summary>
    /// <param name="data">The source data.</param>
    /// <returns>A new dataframe.</returns>
    public static ForgeDataFrame DataFrame(IEnumerable data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var rows = new List<IDictionary<string, object?>>();
        foreach (var item in data)
            rows.Add(ToRow(item));
        return new ForgeDataFrame(rows);
    }

    /// <summary>
    /// Creates a dataframe from a dictionary of column names to values.
    /// </summary>
    /// <param name="columns">Column-oriented data.</param>
    /// <returns>A new dataframe.</returns>
    public static ForgeDataFrame DataFrame(IDictionary<string, IEnumerable<object?>> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var materialized = columns.ToDictionary(x => x.Key, x => x.Value?.ToList() ?? [], StringComparer.OrdinalIgnoreCase);
        var count = materialized.Count == 0 ? 0 : materialized.Max(x => x.Value.Count);
        var rows = new List<IDictionary<string, object?>>(count);
        for (var i = 0; i < count; i++)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in materialized)
                row[column.Key] = i < column.Value.Count ? column.Value[i] : null;
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>
    /// Creates a series from a sequence of values.
    /// </summary>
    /// <param name="values">The series values.</param>
    /// <param name="name">The optional series name.</param>
    /// <returns>A new series.</returns>
    public static ForgeSeries Series(IEnumerable<object?> values, string name = "Value") => new(name, values);

    /// <summary>
    /// Reads a CSV file into a dataframe.
    /// </summary>
    public static ForgeDataFrame ReadCsv(string path, bool hasHeader = true, char delimiter = ',') => ForgeDataFrame.FromCsv(path, hasHeader, delimiter);

    /// <summary>
    /// Reads a CSV file into a dataframe asynchronously.
    /// </summary>
    public static ValueTask<ForgeDataFrame> ReadCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(path, hasHeader, delimiter, cancellationToken);

    /// <summary>
    /// Reads the first worksheet from a minimal XLSX workbook into a dataframe.
    /// </summary>
    /// <remarks>
    /// This reader supports the XLSX produced by <see cref="ForgePandasExtensions.ToExcel"/> and common simple worksheets.
    /// Complex formulas, styles and merged cells are intentionally ignored.
    /// </remarks>
    public static ForgeDataFrame ReadExcel(string path, bool hasHeader = true, string? sheetName = null)
    {
        using var archive = ZipFile.OpenRead(path);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase));
        if (sheetEntry is null)
            return ForgeDataFrame.Empty;

        using var stream = sheetEntry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var xml = reader.ReadToEnd();
        var rowMatches = Regex.Matches(xml, "<row[^>]*>(.*?)</row>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var table = new List<List<object?>>();
        foreach (Match rowMatch in rowMatches)
        {
            var row = new List<object?>();
            foreach (Match cell in Regex.Matches(rowMatch.Groups[1].Value, "<c([^>]*)>(.*?)</c>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                var attrs = cell.Groups[1].Value;
                var inner = cell.Groups[2].Value;
                var valueMatch = Regex.Match(inner, "<v[^>]*>(.*?)</v>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var raw = valueMatch.Success ? SecurityElement.FromString("<x>" + valueMatch.Groups[1].Value + "</x>")?.Text ?? valueMatch.Groups[1].Value : string.Empty;
                object? value = raw;
                if (attrs.Contains("t=\"s\"", StringComparison.OrdinalIgnoreCase) && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                    value = sharedStrings[sharedIndex];
                else if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                    value = number;
                else if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dto))
                    value = dto;
                row.Add(value);
            }
            table.Add(row);
        }

        if (table.Count == 0)
            return ForgeDataFrame.Empty;

        var headers = hasHeader
            ? table[0].Select((x, i) => string.IsNullOrWhiteSpace(Convert.ToString(x, CultureInfo.InvariantCulture)) ? $"Column{i + 1}" : Convert.ToString(x, CultureInfo.InvariantCulture)!).ToArray()
            : Enumerable.Range(1, table.Max(r => r.Count)).Select(i => $"Column{i}").ToArray();
        var start = hasHeader ? 1 : 0;
        var rows = new List<IDictionary<string, object?>>();
        for (var i = start; i < table.Count; i++)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Length; c++)
                row[headers[c]] = c < table[i].Count ? table[i][c] : null;
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>
    /// Converts a value to <see cref="DateTimeOffset"/> using invariant culture.
    /// </summary>
    public static DateTimeOffset? ToDateTime(object? value)
    {
        if (value is null or DBNull)
            return null;
        if (value is DateTimeOffset dto)
            return dto;
        if (value is DateTime dt)
            return new DateTimeOffset(dt);
        return DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : null;
    }

    internal static IDictionary<string, object?> ToRow(object? item)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (item is null)
        {
            row["Value"] = null;
            return row;
        }

        if (item is IDictionary<string, object?> typed)
            return new Dictionary<string, object?>(typed, StringComparer.OrdinalIgnoreCase);

        if (item is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
                row[Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty] = entry.Value;
            return row;
        }

        var type = item.GetType();
        if (type.IsPrimitive || item is string || item is decimal || item is DateTime || item is DateTimeOffset || item is Guid)
        {
            row["Value"] = item;
            return row;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0)
                continue;
            row[property.Name] = property.GetValue(item);
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            row[field.Name] = field.GetValue(item);

        return row.Count == 0 ? new Dictionary<string, object?> { ["Value"] = item } : row;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return [];
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var xml = reader.ReadToEnd();
        return Regex.Matches(xml, "<t[^>]*>(.*?)</t>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
            .Select(m => SecurityElement.FromString("<x>" + m.Groups[1].Value + "</x>")?.Text ?? m.Groups[1].Value)
            .ToList();
    }
}
