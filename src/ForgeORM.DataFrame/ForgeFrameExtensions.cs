using System.Collections;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

public static class ForgeFrameExtensions
{
    public static ForgeFrameQuery<T> Frame<T>(this ForgeDb db) => new(db);
    public static ForgeFrameQuery<T> Frame<T>(this ForgeDbContext db) => new(db);

    public static ForgeDataFrame ToForgeFrame<T>(this IEnumerable<T> rows)
        => new(rows.Select(ToDictionary));

    public static ForgeDataFrame ReadCsv(string path, bool hasHeader = true, char delimiter = ',')
        => ForgeDataFrame.FromCsv(path, hasHeader, delimiter);

    public static Task<ForgeDataFrame> ReadCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(path, hasHeader, delimiter, cancellationToken);

    public static Task<ForgeDataFrame> ReadCsvAsync(Stream stream, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(stream, hasHeader, delimiter, cancellationToken);

    public static ForgeDataFrame ReadJson(string path)
        => ForgeDataFrame.FromJson(path);

    public static Task<ForgeDataFrame> ReadJsonAsync(string path, CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromJsonAsync(path, cancellationToken);

    public static Task<ForgeDataFrame> ReadJsonAsync(Stream stream, CancellationToken cancellationToken = default)
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

public sealed class ForgeFrameQuery<T>
{
    private readonly ForgeDb _db;
    private string? _sql;
    private object? _parameters;
    private string? _table;
    private readonly List<string> _where = [];
    private string? _orderBy;

    internal ForgeFrameQuery(ForgeDb db) => _db = db;

    public ForgeFrameQuery<T> From(string table) { _table = table; return this; }
    public ForgeFrameQuery<T> FromSql(string sql, object? parameters = null) { _sql = sql; _parameters = parameters; return this; }
    public ForgeFrameQuery<T> WhereSql(string condition) { _where.Add(condition); return this; }
    public ForgeFrameQuery<T> OrderBy(string orderBy) { _orderBy = orderBy; return this; }

    public async Task<ForgeDataFrame> ToFrameAsync(CancellationToken cancellationToken = default)
    {
        var sql = BuildSql();
        var rows = await _db.QueryDynamicAsync(sql: sql, parameters: _parameters, cancellationToken: cancellationToken);
        return new ForgeDataFrame(rows);
    }

    public ForgeDataFrame ToFrame()
    {
        var rows = _db.QueryDynamicAsync(sql: BuildSql(), parameters: _parameters).GetAwaiter().GetResult();
        return new ForgeDataFrame(rows);
    }

    private string BuildSql()
    {
        var sql = !string.IsNullOrWhiteSpace(_sql) ? _sql! : $"SELECT * FROM {_table ?? typeof(T).Name}";
        if (_where.Count > 0) sql += " WHERE " + string.Join(" AND ", _where);
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql += " ORDER BY " + _orderBy;
        return sql;
    }
}
