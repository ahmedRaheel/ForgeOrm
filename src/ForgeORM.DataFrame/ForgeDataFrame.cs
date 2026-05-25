using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ForgeORM.Core;
using Microsoft.Data.Analysis;


namespace ForgeORM.DataFrame;

public sealed class ForgeDataFrame
{
    private readonly List<Dictionary<string, object?>> _rows;

    /// <summary>
    /// Executes the ForgeDataFrame operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the ForgeDataFrame operation.</returns>
    public ForgeDataFrame(IEnumerable<IDictionary<string, object?>> rows)
    {
        _rows = rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows => _rows;
    public IReadOnlyList<string> Columns => _rows.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    public int RowCount => _rows.Count;

    public static ForgeDataFrame Empty { get; } = new([]);
    /// <summary>
    /// Executes the FromCsvAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FromCsvAsync operation.</returns>
    public static async ValueTask<ForgeDataFrame> FromCsvAsync(
    Stream stream,
    CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return FromCsvText(text);
    }

    /// <summary>
    /// Executes the FromJsonAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FromJsonAsync operation.</returns>
    public static async ValueTask<ForgeDataFrame> FromJsonAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        return FromJsonText(json);
    }
    /// <summary>
    /// Executes the FromCsv operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <returns>The result of the FromCsv operation.</returns>
    public static ForgeDataFrame FromCsv(string path, bool hasHeader = true, char delimiter = ',')
        => FromCsvText(File.ReadAllText(path), hasHeader, delimiter);

    /// <summary>
    /// Executes the FromCsvAsync operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FromCsvAsync operation.</returns>
    public static async ValueTask<ForgeDataFrame> FromCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => FromCsvText(await File.ReadAllTextAsync(path, cancellationToken), hasHeader, delimiter);

    /// <summary>
    /// Executes the FromCsvAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FromCsvAsync operation.</returns>
    public static async ValueTask<ForgeDataFrame> FromCsvAsync(Stream stream, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var csv = await reader.ReadToEndAsync(cancellationToken);
        return FromCsvText(csv, hasHeader, delimiter);
    }

    /// <summary>
    /// Executes the FromCsvText operation.
    /// </summary>
    /// <param name="csv">The csv value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <returns>The result of the FromCsvText operation.</returns>
    public static ForgeDataFrame FromCsvText(string csv, bool hasHeader = true, char delimiter = ',')
    {
        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
            return Empty;

        var first = ParseCsvLine(lines[0], delimiter);
        var headers = hasHeader
            ? first.Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Column{i + 1}" : h.Trim()).ToList()
            : first.Select((_, i) => $"Column{i + 1}").ToList();

        var dataLines = hasHeader ? lines.Skip(1) : lines;
        var rows = new List<IDictionary<string, object?>>();

        foreach (var line in dataLines)
        {
            var values = ParseCsvLine(line, delimiter);
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
                row[headers[i]] = i < values.Count ? NormalizeValue(InferTextValue(values[i])) : null;
            rows.Add(row);
        }

        return new ForgeDataFrame(rows);
    }

    /// <summary>
    /// Executes the FromJson operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <returns>The result of the FromJson operation.</returns>
    public static ForgeDataFrame FromJson(string path)
        => FromJsonText(File.ReadAllText(path));

    /// <summary>
    /// Executes the FromJsonAsync operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FromJsonAsync operation.</returns>
    public static async ValueTask<ForgeDataFrame> FromJsonAsync(string path, CancellationToken cancellationToken = default)
        => FromJsonText(await File.ReadAllTextAsync(path, cancellationToken));

    /// <summary>
    /// Executes the FromJsonv1Async operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FromJsonv1Async operation.</returns>
    public static async ValueTask<ForgeDataFrame> FromJsonv1Async(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        return FromJsonText(json);
    }

    /// <summary>
    /// Executes the FromJsonText operation.
    /// </summary>
    /// <param name="json">The json value.</param>
    /// <returns>The result of the FromJsonText operation.</returns>
    public static ForgeDataFrame FromJsonText(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = new List<IDictionary<string, object?>>();

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in doc.RootElement.EnumerateArray())
                rows.Add(JsonObjectToRow(item));
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            rows.Add(JsonObjectToRow(doc.RootElement));
        }
        else
        {
            rows.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Value"] = NormalizeValue(JsonElementToValue(doc.RootElement))
            });
        }

        return new ForgeDataFrame(rows);
    }
    /// <summary>
    /// Executes the FromCsvAsync operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FromCsvAsync operation.</returns>
    public static async ValueTask<ForgeDataFrame> FromCsvAsync(
    string path,
    CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await FromCsvAsync(stream, cancellationToken);
    }
    
    /// <summary>
    /// Executes the ToTable operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="createIfNotExists">The createIfNotExists value.</param>
    /// <param name="dropIfExists">The dropIfExists value.</param>
    /// <returns>The result of the ToTable operation.</returns>
    public int ToTable(ForgeDb db, string tableName, bool createIfNotExists = true, bool dropIfExists = false)
    {
        return ToTableAsync(db, tableName, createIfNotExists, dropIfExists).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the ToTableAsync operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="createIfNotExists">The createIfNotExists value.</param>
    /// <param name="dropIfExists">The dropIfExists value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToTableAsync operation.</returns>
    public async ValueTask<int> ToTableAsync(
        ForgeDb db,
        string tableName,
        bool createIfNotExists = true,
        bool dropIfExists = false,
        CancellationToken cancellationToken = default)
    {
        var columns = Columns.ToList();
        if (columns.Count == 0)
            return 0;

        var escapedTable = EscapeTableName(tableName);
        var objectName = tableName.Replace("[", string.Empty).Replace("]", string.Empty).Replace("'", "''");

        if (dropIfExists)
        {
            await db.ExecuteAsync(
                $"IF OBJECT_ID(N'{objectName}', N'U') IS NOT NULL DROP TABLE {escapedTable};",
                cancellationToken: cancellationToken);
        }

        if (createIfNotExists)
        {
            var definitions = string.Join(", ", columns.Select(c => $"{EscapeIdentifier(c)} {InferSqlType(c)} NULL"));
            await db.ExecuteAsync(
                $"IF OBJECT_ID(N'{objectName}', N'U') IS NULL CREATE TABLE {escapedTable} ({definitions});",
                cancellationToken: cancellationToken);
        }

        var inserted = 0;
        foreach (var row in _rows)
        {
            var insertColumns = string.Join(", ", columns.Select(EscapeIdentifier));
            var parameterNames = string.Join(", ", columns.Select((_, i) => "@p" + i));
            var parameters = columns.Select((c, i) => new KeyValuePair<string, object?>("p" + i, ToDatabaseValueForColumn(c, Get(row, c))))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

            inserted += await db.ExecuteAsync(
                $"INSERT INTO {escapedTable} ({insertColumns}) VALUES ({parameterNames});",
                parameters,
                cancellationToken: cancellationToken);
        }

        return inserted;
    }

    /// <summary>
    /// Executes the ToDictionaries operation.
    /// </summary>
    /// <returns>The result of the ToDictionaries operation.</returns>
    public IReadOnlyList<IDictionary<string, object?>> ToDictionaries()
        => _rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).Cast<IDictionary<string, object?>>().ToList();


    /// <summary>
    /// Executes the Head operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Head operation.</returns>
    public ForgeDataFrame Head(int count = 5) => new(_rows.Take(Math.Max(count, 0)));
    /// <summary>
    /// Executes the Tail operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Tail operation.</returns>
    public ForgeDataFrame Tail(int count = 5) => new(_rows.Skip(Math.Max(0, _rows.Count - Math.Max(count, 0))));

    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Select operation.</returns>
    public ForgeDataFrame Select(params string[] columns)
        => new(_rows.Select(row => columns.ToDictionary(c => c, c => row.TryGetValue(c, out var v) ? v : null, StringComparer.OrdinalIgnoreCase)));

    /// <summary>
    /// Executes the Rename operation.
    /// </summary>
    /// <param name="oldName">The oldName value.</param>
    /// <param name="newName">The newName value.</param>
    /// <returns>The result of the Rename operation.</returns>
    public ForgeDataFrame Rename(string oldName, string newName)
    {
        foreach (var row in _rows)
        {
            if (!row.TryGetValue(oldName, out var value)) continue;
            row.Remove(oldName);
            row[newName] = value;
        }
        return this;
    }

    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Where operation.</returns>
    public ForgeDataFrame Where(Func<IReadOnlyDictionary<string, object?>, bool> predicate)
        => new(_rows.Where(r => predicate(r)));

    /// <summary>
    /// Executes the SortBy operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="descending">The descending value.</param>
    /// <returns>The result of the SortBy operation.</returns>
    public ForgeDataFrame SortBy(string column, bool descending = false)
    {
        var sorted = descending
            ? _rows.OrderByDescending(x => x.TryGetValue(column, out var v) ? v : null, Comparer<object?>.Create(CompareValues))
            : _rows.OrderBy(x => x.TryGetValue(column, out var v) ? v : null, Comparer<object?>.Create(CompareValues));
        return new(sorted);
    }

    /// <summary>
    /// Executes the Assign operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="valueFactory">The valueFactory value.</param>
    /// <returns>The result of the Assign operation.</returns>
    public ForgeDataFrame Assign(string column, Func<IReadOnlyDictionary<string, object?>, object?> valueFactory)
    {
        foreach (var row in _rows)
            row[column] = valueFactory(row);
        return this;
    }

    /// <summary>
    /// Executes the FillNa operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the FillNa operation.</returns>
    public ForgeDataFrame FillNa(object? value, params string[] columns)
    {
        var targetColumns = columns.Length == 0 ? Columns : columns;
        foreach (var row in _rows)
            foreach (var c in targetColumns)
                if (!row.TryGetValue(c, out var current) || current is null || current is DBNull)
                    row[c] = value;
        return this;
    }

    /// <summary>
    /// Executes the DropNa operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the DropNa operation.</returns>
    public ForgeDataFrame DropNa(params string[] columns)
    {
        var targetColumns = columns.Length == 0 ? Columns : columns;
        return new(_rows.Where(row => targetColumns.All(c => row.TryGetValue(c, out var v) && v is not null && v is not DBNull)));
    }

    /// <summary>
    /// Executes the DropDuplicates operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the DropDuplicates operation.</returns>
    public ForgeDataFrame DropDuplicates(params string[] columns)
    {
        var targetColumns = columns.Length == 0 ? Columns : columns;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return new(_rows.Where(row => seen.Add(string.Join("|", targetColumns.Select(c => row.TryGetValue(c, out var v) ? Convert.ToString(v, CultureInfo.InvariantCulture) : "")))));
    }

    /// <summary>
    /// Executes the GroupBy operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    public ForgeGroupBy GroupBy(params string[] columns) => new(this, columns);

    /// <summary>
    /// Executes the PivotTable operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <param name="columns">The columns value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="aggregate">The aggregate value.</param>
    /// <returns>The result of the PivotTable operation.</returns>
    public ForgeDataFrame PivotTable(string rows, string columns, string values, ForgeAgg aggregate)
    {
        var columnKeys = _rows.Select(r => ToKey(Get(r, columns))).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var groups = _rows.GroupBy(r => ToKey(Get(r, rows)), StringComparer.OrdinalIgnoreCase);
        var result = new List<IDictionary<string, object?>>();

        foreach (var group in groups)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [rows] = group.Key };
            foreach (var col in columnKeys)
            {
                var slice = group.Where(r => string.Equals(ToKey(Get(r, columns)), col, StringComparison.OrdinalIgnoreCase));
                row[col] = aggregate.Compute(slice.Select(r => Get(r, values)));
            }
            result.Add(row);
        }

        return new(result);
    }

    /// <summary>
    /// Executes the Melt operation.
    /// </summary>
    /// <param name="stringidVars">The stringidVars value.</param>
    /// <param name="stringvalueVars">The stringvalueVars value.</param>
    /// <param name="variableName">The variableName value.</param>
    /// <param name="valueName">The valueName value.</param>
    /// <returns>The result of the Melt operation.</returns>
    public ForgeDataFrame Melt(string[] idVars, string[] valueVars, string variableName = "variable", string valueName = "value")
    {
        var result = new List<IDictionary<string, object?>>();
        foreach (var row in _rows)
        {
            foreach (var valueVar in valueVars)
            {
                var item = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in idVars) item[id] = Get(row, id);
                item[variableName] = valueVar;
                item[valueName] = Get(row, valueVar);
                result.Add(item);
            }
        }
        return new(result);
    }

    /// <summary>
    /// Executes the Merge operation.
    /// </summary>
    /// <param name="right">The right value.</param>
    /// <param name="leftOn">The leftOn value.</param>
    /// <param name="rightOn">The rightOn value.</param>
    /// <param name="join">The join value.</param>
    /// <returns>The result of the Merge operation.</returns>
    public ForgeDataFrame Merge(ForgeDataFrame right, string leftOn, string rightOn, ForgeJoinKind join = ForgeJoinKind.Inner)
    {
        var result = new List<IDictionary<string, object?>>();
        var rightLookup = right._rows.GroupBy(r => ToKey(Get(r, rightOn)), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var left in _rows)
        {
            var key = ToKey(Get(left, leftOn));
            if (rightLookup.TryGetValue(key, out var matches))
            {
                foreach (var match in matches)
                    result.Add(Combine(left, match, "right_"));
            }
            else if (join is ForgeJoinKind.Left or ForgeJoinKind.Full)
            {
                result.Add(new Dictionary<string, object?>(left, StringComparer.OrdinalIgnoreCase));
            }
        }

        if (join is ForgeJoinKind.Right or ForgeJoinKind.Full)
        {
            var leftKeys = _rows.Select(r => ToKey(Get(r, leftOn))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var rightRow in right._rows.Where(r => !leftKeys.Contains(ToKey(Get(r, rightOn)))))
                result.Add(new Dictionary<string, object?>(rightRow, StringComparer.OrdinalIgnoreCase));
        }

        return new(result);
    }

    /// <summary>
    /// Executes the Rolling operation.
    /// </summary>
    /// <param name="valueColumn">The valueColumn value.</param>
    /// <param name="window">The window value.</param>
    /// <param name="outputColumn">The outputColumn value.</param>
    /// <param name="aggregate">The aggregate value.</param>
    /// <returns>The result of the Rolling operation.</returns>
    public ForgeDataFrame Rolling(string valueColumn, int window, string outputColumn, ForgeAgg aggregate)
    {
        var result = _rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).ToList();
        for (var i = 0; i < result.Count; i++)
        {
            var slice = result.Skip(Math.Max(0, i - window + 1)).Take(Math.Min(window, i + 1)).Select(r => Get(r, valueColumn));
            result[i][outputColumn] = aggregate.Compute(slice);
        }
        return new(result);
    }

    /// <summary>
    /// Executes the Describe operation.
    /// </summary>
    /// <param name="stringnumericColumns">The stringnumericColumns value.</param>
    /// <returns>The result of the Describe operation.</returns>
    public ForgeDataFrame Describe(params string[] numericColumns)
    {
        var cols = numericColumns.Length == 0 ? Columns.ToArray() : numericColumns;
        var result = new List<IDictionary<string, object?>>();
        foreach (var col in cols)
        {
            var values = _rows.Select(r => ToDecimal(Get(r, col))).Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();
            if (values.Count == 0) continue;
            result.Add(new Dictionary<string, object?>
            {
                ["Column"] = col,
                ["Count"] = values.Count,
                ["Mean"] = values.Average(),
                ["Min"] = values.Min(),
                ["P25"] = Percentile(values, 0.25m),
                ["Median"] = Percentile(values, 0.50m),
                ["P75"] = Percentile(values, 0.75m),
                ["Max"] = values.Max(),
                ["StdDev"] = StdDev(values)
            });
        }
        return new(result);
    }

    /// <summary>
    /// Executes the ToMicrosoftDataFrame operation.
    /// </summary>
    /// <returns>The result of the ToMicrosoftDataFrame operation.</returns>
    public Microsoft.Data.Analysis.DataFrame ToMicrosoftDataFrame()
    {
        var columns = new List<DataFrameColumn>();
        foreach (var col in Columns)
        {
            var values = _rows.Select(r => Get(r, col)).ToList();
            columns.Add(CreateColumn(col, values));
        }
        return new Microsoft.Data.Analysis.DataFrame(columns);
    }

    /// <summary>
    /// Executes the Get operation.
    /// </summary>
    /// <param name="row">The row value.</param>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the Get operation.</returns>
    public static object? Get(IReadOnlyDictionary<string, object?> row, string column)
        => row.TryGetValue(column, out var value) ? value : null;

    private static IDictionary<string, object?> Combine(IDictionary<string, object?> left, IDictionary<string, object?> right, string rightPrefix)
    {
        var row = new Dictionary<string, object?>(left, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in right)
        {
            var key = row.ContainsKey(pair.Key) ? rightPrefix + pair.Key : pair.Key;
            row[key] = pair.Value;
        }
        return row;
    }

    private static string ToKey(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    internal static decimal? ToDecimal(object? value)
    {
        if (value is null or DBNull) return null;
        try { return Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    internal static decimal Percentile(IReadOnlyList<decimal> orderedValues, decimal percentile)
    {
        if (orderedValues.Count == 0) return 0;
        if (orderedValues.Count == 1) return orderedValues[0];
        var position = (orderedValues.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper) return orderedValues[lower];
        var weight = position - lower;
        return orderedValues[lower] + (orderedValues[upper] - orderedValues[lower]) * weight;
    }

    private static decimal StdDev(IReadOnlyList<decimal> values)
    {
        if (values.Count <= 1) return 0;
        var avg = values.Average();
        var variance = values.Sum(v => (v - avg) * (v - avg)) / (values.Count - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    private static int CompareValues(object? x, object? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        if (x is IComparable comparable) return comparable.CompareTo(y);
        return string.Compare(Convert.ToString(x, CultureInfo.InvariantCulture), Convert.ToString(y, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }




    private string InferSqlType(string column)
    {
        var values = _rows
            .Select(r => NormalizeValue(Get(r, column)))
            .Where(v => v is not null and not DBNull)
            .Take(500)
            .ToList();

        if (values.Count == 0) return "NVARCHAR(MAX)";

        if (values.All(v => IsIntLike(v))) return "INT";
        if (values.All(v => IsLongLike(v))) return "BIGINT";
        if (values.All(v => IsDecimalLike(v))) return "DECIMAL(38, 10)";
        if (values.All(v => IsDoubleLike(v))) return "FLOAT";
        if (values.All(v => IsBoolLike(v))) return "BIT";
        if (values.All(v => IsDateTimeOffsetLike(v))) return "DATETIMEOFFSET";
        if (values.All(v => IsDateTimeLike(v))) return "DATETIME2";
        if (values.All(v => IsGuidLike(v))) return "UNIQUEIDENTIFIER";

        return "NVARCHAR(MAX)";
    }

    private object? ToDatabaseValueForColumn(string column, object? value)
    {
        value = NormalizeValue(value);
        if (value is null or DBNull) return null;

        var sqlType = InferSqlType(column);
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();

        if (sqlType == "INT")
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;

        if (sqlType == "BIGINT")
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : null;

        if (sqlType.StartsWith("DECIMAL", StringComparison.OrdinalIgnoreCase))
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;

        if (sqlType == "FLOAT")
            return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f) ? f : null;

        if (sqlType == "BIT")
            return TryConvertBool(value, out var b) ? b : null;

        if (sqlType == "DATETIMEOFFSET")
            return TryConvertDateTimeOffset(value, out var dto) ? dto : null;

        if (sqlType == "DATETIME2")
            return TryConvertDateTime(value, out var dt) ? dt : null;

        if (sqlType == "UNIQUEIDENTIFIER")
            return Guid.TryParse(text, out var g) ? g : null;

        return ToDatabaseValue(value);
    }

    private static object? ToDatabaseValue(object? value)
    {
        value = NormalizeValue(value);
        if (value is null or DBNull) return null;
        if (value is JsonElement element) return NormalizeValue(JsonElementToValue(element));
        if (value is Enum) return value.ToString();
        return value;
    }

    private static bool IsNullLike(object? value)
    {
        if (value is null or DBNull) return true;
        if (value is JsonElement element)
            return element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                || (element.ValueKind == JsonValueKind.String && IsNullLike(element.GetString()));

        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(text)
            || text == "?"
            || text == "-"
            || text == "--"
            || text.Equals("null", StringComparison.OrdinalIgnoreCase)
            || text.Equals("n/a", StringComparison.OrdinalIgnoreCase)
            || text.Equals("na", StringComparison.OrdinalIgnoreCase)
            || text.Equals("nan", StringComparison.OrdinalIgnoreCase)
            || text.Equals("none", StringComparison.OrdinalIgnoreCase);
    }

    private static object? NormalizeValue(object? value) => IsNullLike(value) ? null : value;

    private static bool IsIntLike(object? value)
    {
        if (value is int or short or byte) return true;
        return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsLongLike(object? value)
    {
        if (value is long or int or short or byte) return true;
        return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsDecimalLike(object? value)
    {
        if (value is decimal or int or long or short or byte) return true;
        return decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsDoubleLike(object? value)
    {
        if (value is double or float or decimal or int or long or short or byte) return true;
        return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsBoolLike(object? value) => TryConvertBool(value, out _);

    private static bool TryConvertBool(object? value, out bool result)
    {
        if (value is bool b) { result = b; return true; }
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        if (bool.TryParse(text, out result)) return true;
        if (text == "1") { result = true; return true; }
        if (text == "0") { result = false; return true; }
        result = false;
        return false;
    }

    private static bool IsDateTimeOffsetLike(object? value) => TryConvertDateTimeOffset(value, out _);

    private static bool TryConvertDateTimeOffset(object? value, out DateTimeOffset result)
    {
        if (value is DateTimeOffset dto) { result = dto; return true; }
        return DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result);
    }

    private static bool IsDateTimeLike(object? value) => TryConvertDateTime(value, out _);

    private static bool TryConvertDateTime(object? value, out DateTime result)
    {
        if (value is DateTime dt) { result = dt; return true; }
        return DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result);
    }

    private static bool IsGuidLike(object? value)
    {
        if (value is Guid) return true;
        return Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out _);
    }

    private static string EscapeTableName(string tableName)
        => string.Join('.', tableName.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(EscapeIdentifier));

    private static string EscapeIdentifier(string name)
        => "[" + name.Trim('[', ']').Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == delimiter && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        result.Add(sb.ToString());
        return result;
    }

    private static object? InferTextValue(string? value)
    {
        if (IsNullLike(value)) return null;
        var text = value!.Trim();
        if (bool.TryParse(text, out var b)) return b;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto)) return dto;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)) return dt;
        if (Guid.TryParse(text, out var g)) return g;
        return text;
    }

    private static IDictionary<string, object?> JsonObjectToRow(JsonElement element)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (element.ValueKind != JsonValueKind.Object)
        {
            row["Value"] = NormalizeValue(JsonElementToValue(element));
            return row;
        }

        foreach (var property in element.EnumerateObject())
            row[property.Name] = NormalizeValue(JsonElementToValue(property.Value));

        return row;
    }

    private static object? JsonElementToValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.String when IsNullLike(element.GetString()) => null,
            JsonValueKind.String when element.TryGetDateTimeOffset(out var dto) => dto,
            JsonValueKind.String when element.TryGetDateTime(out var dt) => dt,
            JsonValueKind.String when element.TryGetGuid(out var g) => g,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array or JsonValueKind.Object => element.GetRawText(),
            _ => element.GetRawText()
        };
    }


    private static DataFrameColumn CreateColumn(string name, IReadOnlyList<object?> values)
    {
        var nonNull = values.FirstOrDefault(v => v is not null and not DBNull);
        var type = nonNull?.GetType();

        if (type == typeof(int))
            return new Int32DataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (int?)null : Convert.ToInt32(v)));

        if (type == typeof(long))
            return new Int64DataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (long?)null : Convert.ToInt64(v)));

        if (type == typeof(float))
            return new SingleDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (float?)null : Convert.ToSingle(v)));

        if (type == typeof(double))
            return new DoubleDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (double?)null : Convert.ToDouble(v)));

        if (type == typeof(decimal))
            return new DoubleDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (double?)null : Convert.ToDouble(v)));

        if (type == typeof(bool))
            return new BooleanDataFrameColumn(
                name,
                values.Select(v => v is null or DBNull ? (bool?)null : Convert.ToBoolean(v)));

        return new StringDataFrameColumn(
            name,
            values.Select(v => v is null or DBNull ? null : v.ToString()));
    }
}
