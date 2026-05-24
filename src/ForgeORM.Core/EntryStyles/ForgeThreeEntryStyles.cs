using System.Linq.Expressions;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Core.EntryStyles;

/// <summary>
/// ForgeORM standard user experience:
/// every major feature should be available through three styles:
/// 1. Fluent/query builder
/// 2. Raw SQL
/// 3. Expression-based
/// </summary>
public enum ForgeEntryStyle
{
    FluentBuilder,
    RawSql,
    Expression
}

/// <summary>
/// Standard query result wrapper for docs, samples and user-friendly APIs.
/// </summary>
public sealed record ForgeEntryStyleResult<T>(
    ForgeEntryStyle Style,
    string Description,
    T Result);

/// <summary>
/// Raw SQL entry object. This gives raw SQL the same terminal methods as query builders.
/// </summary>
public sealed class ForgeRawSqlQuery
{
    private readonly ForgeDb _db;
    private readonly string _sql;
    private readonly object? _parameters;
    private string? _name;

    internal ForgeRawSqlQuery(
        ForgeDb db,
        string sql,
        object? parameters = null)
    {
        _db = db;
        _sql = sql;
        _parameters = parameters;
    }

    public ForgeRawSqlQuery Named(string name)
    {
        _name = name;
        return this;
    }

    public string ToSql() => _sql;

    public ValueTask<IReadOnlyList<T>> ToListAsync<T>(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryAsync<T>(
            _sql,
            _parameters,
            cancellationToken: cancellationToken);            
    }

    public ValueTask<IReadOnlyList<Dictionary<string, object?>>> ToDictionaryAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryDictionaryAsync(
            _sql,
            _parameters,
            cancellationToken: cancellationToken);
    }

    public ValueTask<ForgeJsonProjection> ToJsonAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryJsonProjectionAsync(
            _sql,
            _parameters,
            _name,
            cancellationToken);
    }

    public ValueTask<ForgeTabularResult> ToDataFrameAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryDataFrameAsync(
            _sql,
            _parameters,
            _name,
            cancellationToken);
    }

    public ValueTask<string> ToCsvAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryCsvAsync(
            _sql,
            _parameters,
            cancellationToken);
    }
}

/// <summary>
/// Raw SQL entry-point extensions.
/// </summary>
public static class ForgeRawSqlEntryExtensions
{
    /// <summary>
    /// Starts a raw SQL query with terminal materializers.
    /// </summary>
    public static ForgeRawSqlQuery Sql(
        this ForgeDb db,
        string sql,
        object? parameters = null)
    {
        return new ForgeRawSqlQuery(db, sql, parameters);
    }

    /// <summary>
    /// Alias for developers who prefer Raw().
    /// </summary>
    public static ForgeRawSqlQuery Raw(
        this ForgeDb db,
        string sql,
        object? parameters = null)
    {
        return new ForgeRawSqlQuery(db, sql, parameters);
    }
}

/// <summary>
/// Expression query convenience helpers that expose the same terminal mindset.
/// This intentionally sits beside the existing Query<T>() builder.
/// </summary>
public static class ForgeExpressionEntryExtensions
{
    public static ForgeExpressionQuery<TEntity> Expression<TEntity>(
        this ForgeDb db)
        where TEntity : class, new()
    {
        return new ForgeExpressionQuery<TEntity>(db);
    }
}

/// <summary>
/// Minimal expression-first query object with terminal materializers.
/// </summary>
public sealed class ForgeExpressionQuery<TEntity>
    where TEntity : class, new()
{
    private readonly ForgeDb _db;
    private string _tableName = typeof(TEntity).Name.EndsWith("s", StringComparison.OrdinalIgnoreCase)
        ? typeof(TEntity).Name
        : typeof(TEntity).Name + "s";

    private readonly List<string> _where = [];
    private readonly List<string> _orderBy = [];
    private int? _take;

    internal ForgeExpressionQuery(ForgeDb db)
    {
        _db = db;
    }

    public ForgeExpressionQuery<TEntity> From(string table)
    {
        _tableName = table;
        return this;
    }

    public ForgeExpressionQuery<TEntity> Where<TValue>(
        Expression<Func<TEntity, TValue>> column,
        string op,
        object? value)
    {
        var name = GetMemberName(column.Body);
        var parameterName = "@p" + _where.Count;
        _where.Add($"{name} {op} {FormatSqlValue(value)}");
        return this;
    }

    public ForgeExpressionQuery<TEntity> OrderBy<TValue>(
        Expression<Func<TEntity, TValue>> column)
    {
        _orderBy.Add(GetMemberName(column.Body) + " ASC");
        return this;
    }

    public ForgeExpressionQuery<TEntity> OrderByDescending<TValue>(
        Expression<Func<TEntity, TValue>> column)
    {
        _orderBy.Add(GetMemberName(column.Body) + " DESC");
        return this;
    }

    public ForgeExpressionQuery<TEntity> Take(int take)
    {
        _take = take;
        return this;
    }

    public string ToSql()
    {
        var sql = _take.HasValue
            ? $"SELECT TOP ({_take.Value}) * FROM {_tableName}"
            : $"SELECT * FROM {_tableName}";

        if (_where.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", _where);
        }

        if (_orderBy.Count > 0)
        {
            sql += " ORDER BY " + string.Join(", ", _orderBy);
        }

        return sql;
    }

    public ValueTask<IReadOnlyList<TEntity>> ToListAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.Sql(ToSql()).ToListAsync<TEntity>(cancellationToken);
    }

    public ValueTask<IReadOnlyList<Dictionary<string, object?>>> ToDictionaryAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.Sql(ToSql()).ToDictionaryAsync(cancellationToken);
    }

    public ValueTask<ForgeJsonProjection> ToJsonAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.Sql(ToSql()).Named(typeof(TEntity).Name).ToJsonAsync(cancellationToken);
    }

    public ValueTask<ForgeTabularResult> ToDataFrameAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.Sql(ToSql()).Named(typeof(TEntity).Name).ToDataFrameAsync(cancellationToken);
    }

    public ValueTask<string> ToCsvAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.Sql(ToSql()).ToCsvAsync(cancellationToken);
    }

    private static string GetMemberName(Expression expression)
    {
        while (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        return expression is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Only simple member expressions are supported by this convenience expression entry API.");
    }

    private static string FormatSqlValue(object? value)
    {
        if (value is null)
        {
            return "NULL";
        }

        return value switch
        {
            string s => "'" + s.Replace("'", "''") + "'",
            DateTime dt => "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'",
            DateTimeOffset dto => "'" + dto.ToString("yyyy-MM-dd HH:mm:ss zzz") + "'",
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt32(e).ToString(),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"
        };
    }
}
