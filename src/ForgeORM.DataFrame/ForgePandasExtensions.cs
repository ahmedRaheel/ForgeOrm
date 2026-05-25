using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeORM.DataFrame;

/// <summary>
/// Pandas-style extension methods for <see cref="ForgeDataFrame"/>.
/// </summary>
public static class ForgePandasExtensions
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
        var rowOffset = rowRange.GetOffsetAndLength(rows.Count);
        var columnOffset = columnRange.GetOffsetAndLength(columns.Length);
        var selectedColumns = columns.Skip(columnOffset.Offset).Take(columnOffset.Length).ToArray();
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
