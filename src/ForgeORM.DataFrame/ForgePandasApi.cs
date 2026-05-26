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
internal static partial class ForgePandas
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

/// <summary>
/// A lightweight Pandas-like Series abstraction used by ForgeORM analytics.
/// </summary>
public sealed class ForgeSeries
{
    private readonly List<object?> _values;

    /// <summary>
    /// Creates a new series.
    /// </summary>
    public ForgeSeries(string name, IEnumerable<object?> values)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Value" : name;
        _values = values?.ToList() ?? [];
    }

    /// <summary>The series name.</summary>
    public string Name { get; }

    /// <summary>The series values.</summary>
    public IReadOnlyList<object?> Values => _values;

    /// <summary>Returns unique values in order of first occurrence.</summary>
    public IReadOnlyList<object?> Unique() => _values.Distinct(ForgeObjectEqualityComparer.Instance).ToList();

    /// <summary>Returns the number of unique values.</summary>
    public int NUnique(bool dropNa = true) => dropNa
        ? _values.Where(v => v is not null and not DBNull).Distinct(ForgeObjectEqualityComparer.Instance).Count()
        : _values.Distinct(ForgeObjectEqualityComparer.Instance).Count();

    /// <summary>Returns value counts as a dataframe with Value and Count columns.</summary>
    public ForgeDataFrame ValueCounts(bool dropNa = true)
    {
        var rows = _values
            .Where(v => !dropNa || v is not null and not DBNull)
            .GroupBy(v => Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                [Name] = g.First(),
                ["Count"] = g.Count()
            });
        return new ForgeDataFrame(rows);
    }

    /// <summary>Returns the first n values as a series.</summary>
    public ForgeSeries Head(int count = 5) => new(Name, _values.Take(Math.Max(count, 0)));

    /// <summary>Returns the last n values as a series.</summary>
    public ForgeSeries Tail(int count = 5) => new(Name, _values.Skip(Math.Max(0, _values.Count - Math.Max(count, 0))));

    /// <summary>Returns the series as a single-column dataframe.</summary>
    public ForgeDataFrame ToDataFrame() => new(_values.Select(v => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [Name] = v }));
}

/// <summary>
/// Pandas-style extension methods for <see cref="ForgeDataFrame"/>.
/// </summary>
internal static partial class ForgePandasExtensions
{
    /// <summary>Writes the dataframe as CSV.</summary>
    public static void ToCsv(this ForgeDataFrame frame, string path, char delimiter = ',', bool includeHeader = true)
        => File.WriteAllText(path, frame.ToCsvText(delimiter, includeHeader), Encoding.UTF8);

    /// <summary>Writes the dataframe as CSV asynchronously.</summary>
    public static ValueTask ToCsvAsync(this ForgeDataFrame frame, string path, char delimiter = ',', bool includeHeader = true, CancellationToken cancellationToken = default)
        => new (File.WriteAllTextAsync(path, frame.ToCsvText(delimiter, includeHeader), Encoding.UTF8, cancellationToken));

    /// <summary>Returns the dataframe as CSV text.</summary>
    public static string ToCsvText(this ForgeDataFrame frame, char delimiter = ',', bool includeHeader = true)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var columns = frame.Columns.ToArray();
        var sb = new StringBuilder();
        if (includeHeader)
            sb.AppendLine(string.Join(delimiter, columns.Select(c => EscapeCsv(c, delimiter))));
        foreach (var row in frame.Rows)
            sb.AppendLine(string.Join(delimiter, columns.Select(c => EscapeCsv(ForgeDataFrame.Get(row, c), delimiter))));
        return sb.ToString();
    }

    /// <summary>Writes the dataframe to a minimal XLSX workbook.</summary>
    public static void ToExcel(this ForgeDataFrame frame, string path, string sheetName = "Sheet1")
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (File.Exists(path))
            File.Delete(path);

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        AddEntry(archive, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
""");
        AddEntry(archive, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
""");
        AddEntry(archive, "xl/_rels/workbook.xml.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
""");
        AddEntry(archive, "xl/workbook.xml", $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="{Xml(sheetName)}" sheetId="1" r:id="rId1"/></sheets></workbook>
""");
        AddEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(frame));
    }

    /// <summary>Returns a concise dataframe summary similar to pandas info().</summary>
    public static ForgeDataFrame Info(this ForgeDataFrame frame)
    {
        var rows = frame.Columns.Select(column => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Column"] = column,
            ["NonNull"] = frame.Rows.Count(r => ForgeDataFrame.Get(r, column) is not null and not DBNull),
            ["Null"] = frame.Rows.Count(r => ForgeDataFrame.Get(r, column) is null or DBNull),
            ["DType"] = InferDType(frame.Rows.Select(r => ForgeDataFrame.Get(r, column)))
        });
        return new ForgeDataFrame(rows);
    }

    /// <summary>Returns dataframe shape as row count and column count.</summary>
    public static (int Rows, int Columns) Shape(this ForgeDataFrame frame) => (frame.RowCount, frame.Columns.Count);

    /// <summary>Returns a single dataframe column as a series.</summary>
    public static ForgeSeries Series(this ForgeDataFrame frame, string column)
        => new(column, frame.Rows.Select(row => ForgeDataFrame.Get(row, column)));

    /// <summary>Returns value counts for a column.</summary>
    public static ForgeDataFrame ValueCounts(this ForgeDataFrame frame, string column, bool dropNa = true)
        => frame.Series(column).ValueCounts(dropNa);

    /// <summary>Returns unique values for a column.</summary>
    public static IReadOnlyList<object?> Unique(this ForgeDataFrame frame, string column)
        => frame.Series(column).Unique();

    /// <summary>Returns the number of unique values for a column.</summary>
    public static int NUnique(this ForgeDataFrame frame, string column, bool dropNa = true)
        => frame.Series(column).NUnique(dropNa);

    /// <summary>Selects rows by zero-based row indexes and columns by labels, similar to pandas loc for label columns.</summary>
    public static ForgeDataFrame Loc(this ForgeDataFrame frame, IEnumerable<int> rowIndexes, params string[] columns)
    {
        var selectedColumns = columns.Length == 0 ? frame.Columns.ToArray() : columns;
        var indexSet = rowIndexes.ToHashSet();
        var rows = frame.Rows.Select((row, index) => (row, index))
            .Where(x => indexSet.Contains(x.index))
            .Select(x => selectedColumns.ToDictionary(c => c, c => ForgeDataFrame.Get(x.row, c), StringComparer.OrdinalIgnoreCase));
        return new ForgeDataFrame(rows);
    }

    /// <summary>Selects rows and columns by integer positions, similar to pandas iloc.</summary>
    public static ForgeDataFrame ILoc(this ForgeDataFrame frame, Range? rowRange = null, Range? columnRange = null)
    {
        var rows = frame.Rows.ToList();
        var columns = frame.Columns.ToArray();
        var rowOffset = 1;//rowRange.GetOffsetAndLength(rows.Count);
        var columnOffset = 2;//columnRange.GetOffsetAndLength(columns.Length);
        var selectedColumns = 10;//columns.Skip(columnOffset.Offset).Take(columnOffset.Length).ToArray();
        return new ForgeDataFrame(rows.Skip(rowOffset.Offset).Take(rowOffset.Length).Select(row => selectedColumns.ToDictionary(c => c, c => ForgeDataFrame.Get(row, c), StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>Alias for pandas sort_values().</summary>
    public static ForgeDataFrame SortValues(this ForgeDataFrame frame, string by, bool ascending = true)
        => frame.SortBy(by, descending: !ascending);

    /// <summary>Filters rows using a simple boolean expression such as "Status == 'Paid'" or "Total &gt;= 100".</summary>
    public static ForgeDataFrame Query(this ForgeDataFrame frame, string expression)
    {
        var predicate = ForgeFrameQueryExpression.Compile(expression);
        return frame.Where(predicate);
    }

    /// <summary>Returns a boolean mask dataframe showing null values.</summary>
    public static ForgeDataFrame IsNull(this ForgeDataFrame frame)
        => BooleanMask(frame, isNull: true);

    /// <summary>Returns a boolean mask dataframe showing non-null values.</summary>
    public static ForgeDataFrame NotNull(this ForgeDataFrame frame)
        => BooleanMask(frame, isNull: false);

    /// <summary>Pandas-style alias for DropNa.</summary>
    public static ForgeDataFrame DropNaPandas(this ForgeDataFrame frame, params string[] columns) => frame.DropNa(columns);

    /// <summary>Pandas-style alias for FillNa.</summary>
    public static ForgeDataFrame FillNaPandas(this ForgeDataFrame frame, object? value, params string[] columns) => frame.FillNa(value, columns);

    /// <summary>Renames columns using a mapping dictionary.</summary>
    public static ForgeDataFrame Rename(this ForgeDataFrame frame, IDictionary<string, string> columns)
    {
        foreach (var pair in columns)
            frame.Rename(pair.Key, pair.Value);
        return frame;
    }

    /// <summary>Applies a row function and returns a single-column dataframe.</summary>
    public static ForgeDataFrame Apply(this ForgeDataFrame frame, Func<IReadOnlyDictionary<string, object?>, object?> func, string resultColumn = "Value")
    {
        var rows = frame.Rows.Select(row => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [resultColumn] = func(row) });
        return new ForgeDataFrame(rows);
    }

    /// <summary>Applies a column function and returns one row containing the results.</summary>
    public static ForgeDataFrame ApplyColumns(this ForgeDataFrame frame, Func<string, IEnumerable<object?>, object?> func)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in frame.Columns)
            row[column] = func(column, frame.Rows.Select(r => ForgeDataFrame.Get(r, column)));
        return new ForgeDataFrame(new[] { row });
    }

    /// <summary>Concatenates dataframes by rows or columns.</summary>
    public static ForgeDataFrame Concat(IEnumerable<ForgeDataFrame> frames, int axis = 0)
    {
        var materialized = frames?.ToList() ?? [];
        if (axis == 0)
            return new ForgeDataFrame(materialized.SelectMany(f => f.Rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase))));

        var maxRows = materialized.Count == 0 ? 0 : materialized.Max(f => f.RowCount);
        var rows = new List<IDictionary<string, object?>>(maxRows);
        for (var i = 0; i < maxRows; i++)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var frame in materialized)
            {
                if (i >= frame.RowCount)
                    continue;
                foreach (var pair in frame.Rows[i])
                    row[row.ContainsKey(pair.Key) ? $"{pair.Key}_{rows.Count}" : pair.Key] = pair.Value;
            }
            rows.Add(row);
        }
        return new ForgeDataFrame(rows);
    }

    /// <summary>Pandas-style merge overload supporting inner, left, right and full joins.</summary>
    public static ForgeDataFrame Merge(this ForgeDataFrame left, ForgeDataFrame right, string on, ForgeJoinKind how = ForgeJoinKind.Inner)
        => left.Merge(right, on, on, how);

    /// <summary>Pandas-style pivot_table alias.</summary>
    public static ForgeDataFrame PivotTable(this ForgeDataFrame frame, string index, string columns, string values, string aggFunc = "sum")
        => frame.PivotTable(index, columns, values, ForgeAggFromName(aggFunc));

    /// <summary>Creates a lightweight plot specification that can be rendered by a UI layer.</summary>
    public static ForgePlotSpec Plot(this ForgeDataFrame frame, string x, string y, string kind = "line", string? title = null)
        => new(kind, x, y, title ?? $"{kind}: {y} by {x}", frame.Select(x, y).ToDictionaries());

    /// <summary>Converts a column to DateTimeOffset values in-place.</summary>
    public static ForgeDataFrame ToDateTime(this ForgeDataFrame frame, string column)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Column name is required.", nameof(column));

        foreach (var row in frame.Rows)
        {
            if (row is IDictionary<string, object?> mutableRow)
            {
                mutableRow[column] = ForgePandas.ToDateTime(
                    ForgeDataFrame.Get((IReadOnlyDictionary<string, object?>)mutableRow, column));
            }
            else
            {
                throw new InvalidOperationException(
                    "ForgeDataFrame rows must implement IDictionary<string, object?>.");
            }
        }

        return frame;
    }

    private static (int Offset, int Length) GetOffsetAndLength(this Range? range, int length)
     => (range ?? new Range(Index.Start, Index.End)).GetOffsetAndLength(length);
    private static ForgeDataFrame BooleanMask(ForgeDataFrame frame, bool isNull)
    {
        var rows = frame.Rows.Select(row => frame.Columns.ToDictionary(c => c, c => (ForgeDataFrame.Get(row, c) is null or DBNull) == isNull ? (object?)true : false, StringComparer.OrdinalIgnoreCase));
        return new ForgeDataFrame(rows);
    }

    private static string EscapeCsv(object? value, char delimiter)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        var mustQuote = text.Contains(delimiter) || text.Contains('"') || text.Contains('\n') || text.Contains('\r');
        text = text.Replace("\"", "\"\"");
        return mustQuote ? $"\"{text}\"" : text;
    }

    private static string InferDType(IEnumerable<object?> values)
    {
        var first = values.FirstOrDefault(v => v is not null and not DBNull);
        if (first is null)
            return "object";
        var type = first.GetType();
        if (type == typeof(string)) return "string";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "datetime";
        if (type == typeof(bool)) return "bool";
        if (type.IsPrimitive || type == typeof(decimal)) return "number";
        return type.Name;
    }

    private static ForgeAgg ForgeAggFromName(string name)
    {
        return name.Trim().ToLowerInvariant() switch
        {
            "count" => ForgeAgg.Count(),
            "avg" or "average" or "mean" => ForgeAgg.Avg(),
            "min" => ForgeAgg.Min(),
            "max" => ForgeAgg.Max(),
            "median" => ForgeAgg.Median(),
            _ => ForgeAgg.Sum()
        };
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildWorksheetXml(ForgeDataFrame frame)
    {
        var columns = frame.Columns.ToArray();
        var sb = new StringBuilder(4096);
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        sb.Append("<row r=\"1\">");
        for (var c = 0; c < columns.Length; c++)
            AppendCell(sb, 1, c + 1, columns[c]);
        sb.Append("</row>");
        for (var r = 0; r < frame.RowCount; r++)
        {
            sb.Append("<row r=\"").Append(r + 2).Append("\">");
            for (var c = 0; c < columns.Length; c++)
                AppendCell(sb, r + 2, c + 1, ForgeDataFrame.Get(frame.Rows[r], columns[c]));
            sb.Append("</row>");
        }
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static void AppendCell(StringBuilder sb, int row, int column, object? value)
    {
        var cellRef = ColumnName(column) + row.ToString(CultureInfo.InvariantCulture);
        if (value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\"><v>").Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append("</v></c>");
            return;
        }
        sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\"><is><t>").Append(Xml(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)).Append("</t></is></c>");
    }

    private static string ColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static string Xml(string value) => SecurityElement.Escape(value) ?? string.Empty;
}

/// <summary>
/// Lightweight plot metadata produced by ForgeDataFrame.Plot().
/// </summary>
public sealed record ForgePlotSpec(string Kind, string X, string Y, string Title, IReadOnlyList<IDictionary<string, object?>> Data);

internal sealed class ForgeObjectEqualityComparer : IEqualityComparer<object?>
{
    public static readonly ForgeObjectEqualityComparer Instance = new();

    public new bool Equals(object? x, object? y)
        => string.Equals(Convert.ToString(x, CultureInfo.InvariantCulture), Convert.ToString(y, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

    public int GetHashCode(object? obj)
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Convert.ToString(obj, CultureInfo.InvariantCulture) ?? string.Empty);
}

internal static class ForgeFrameQueryExpression
{
    private static readonly Regex BinaryExpression = new(
        "^\\s*(?<column>[A-Za-z_][A-Za-z0-9_ .]*)\\s*(?<op>==|=|!=|>=|<=|>|<|contains|startswith|endswith)\\s*(?<value>.+?)\\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static Func<IReadOnlyDictionary<string, object?>, bool> Compile(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return _ => true;

        var parts = Regex.Split(expression, "\\s+and\\s+", RegexOptions.IgnoreCase)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(CompileSingle)
            .ToArray();
        return row => parts.All(predicate => predicate(row));
    }

    private static Func<IReadOnlyDictionary<string, object?>, bool> CompileSingle(string expression)
    {
        var match = BinaryExpression.Match(expression);
        if (!match.Success)
            throw new ArgumentException($"Unsupported dataframe query expression: {expression}", nameof(expression));

        var column = match.Groups["column"].Value.Trim();
        var op = match.Groups["op"].Value.Trim().ToLowerInvariant();
        var expected = ParseLiteral(match.Groups["value"].Value.Trim());
        return row => Compare(ForgeDataFrame.Get(row, column), expected, op);
    }

    private static object? ParseLiteral(string text)
    {
        if ((text.StartsWith("'", StringComparison.Ordinal) && text.EndsWith("'", StringComparison.Ordinal)) ||
            (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal)))
            return text[1..^1];
        if (string.Equals(text, "null", StringComparison.OrdinalIgnoreCase))
            return null;
        if (bool.TryParse(text, out var b))
            return b;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dto))
            return dto;
        return text;
    }

    private static bool Compare(object? actual, object? expected, string op)
    {
        if (op is "contains" or "startswith" or "endswith")
        {
            var a = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;
            var e = Convert.ToString(expected, CultureInfo.InvariantCulture) ?? string.Empty;
            return op switch
            {
                "contains" => a.Contains(e, StringComparison.OrdinalIgnoreCase),
                "startswith" => a.StartsWith(e, StringComparison.OrdinalIgnoreCase),
                _ => a.EndsWith(e, StringComparison.OrdinalIgnoreCase)
            };
        }

        var cmp = CompareValue(actual, expected);
        return op switch
        {
            "=" or "==" => cmp == 0,
            "!=" => cmp != 0,
            ">" => cmp > 0,
            ">=" => cmp >= 0,
            "<" => cmp < 0,
            "<=" => cmp <= 0,
            _ => false
        };
    }

    private static int CompareValue(object? left, object? right)
    {
        if (left is null or DBNull)
            return right is null or DBNull ? 0 : -1;
        if (right is null or DBNull)
            return 1;
        if (decimal.TryParse(Convert.ToString(left, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var ld) &&
            decimal.TryParse(Convert.ToString(right, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var rd))
            return ld.CompareTo(rd);
        if (DateTimeOffset.TryParse(Convert.ToString(left, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var ldt) &&
            DateTimeOffset.TryParse(Convert.ToString(right, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var rdt))
            return ldt.CompareTo(rdt);
        return string.Compare(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }
}
