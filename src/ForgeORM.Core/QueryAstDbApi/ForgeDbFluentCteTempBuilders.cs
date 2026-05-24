using System.Linq.Expressions;
using System.Text;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>Starts a fluent db.Cte&lt;T&gt;() query.</summary>
    public ForgeDbCteQuery<T> Cte<T>() => new(this);

    /// <summary>Starts a fluent db.TempTable&lt;T&gt;("#Name") builder.</summary>
    public ForgeDbTempTableQuery<T> TempTable<T>(string name) => new(this, name);
}

public sealed class ForgeDbCteQuery<T>
{
    private readonly ForgeDb _db;
    private string? _cteName;
    private string? _cteSql;
    private string? _from;

    internal ForgeDbCteQuery(ForgeDb db) => _db = db;

    public ForgeDbCteQuery<T> With(string name, Func<ForgeDbCteInnerQuery<T>, ForgeDbCteInnerQuery<T>> build)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("CTE name is required.", nameof(name));
        if (build is null) throw new ArgumentNullException(nameof(build));
        _cteName = name;
        _cteSql = build(new ForgeDbCteInnerQuery<T>()).ToSql();
        return this;
    }

    public ForgeDbCteQuery<T> From(string source)
    {
        _from = source;
        return this;
    }

    public string ToSql()
    {
        if (string.IsNullOrWhiteSpace(_cteName) || string.IsNullOrWhiteSpace(_cteSql))
            throw new InvalidOperationException("CTE query requires With(name, query).");
        var from = string.IsNullOrWhiteSpace(_from) ? _cteName : _from;
        return $"WITH {_cteName} AS ({_cteSql}) SELECT * FROM {from}";
    }

    public ValueTask<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
        => _db.QueryAsync<T>(ToSql(), cancellationToken);
}

public sealed class ForgeDbCteInnerQuery<T>
{
    private string _table = typeof(T).Name;
    private readonly List<string> _where = new();

    public ForgeDbCteInnerQuery<T> From<TEntity>()
    {
        _table = typeof(TEntity).Name;
        return this;
    }

    public ForgeDbCteInnerQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeDbFluentExpressionSql.TranslateWhere(predicate));
        return this;
    }

    internal string ToSql()
    {
        var sql = new StringBuilder($"SELECT * FROM {_table}");
        if (_where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        return sql.ToString();
    }
}

public sealed class ForgeDbTempTableQuery<T>
{
    private readonly ForgeDb _db;
    private readonly string _name;
    private string? _sourceSql;

    internal ForgeDbTempTableQuery(ForgeDb db, string name)
    {
        _db = db;
        _name = name.StartsWith("#", StringComparison.Ordinal) ? name : "#" + name;
    }

    public ForgeDbTempTableQuery<T> FromQuery(Func<ForgeDbTempSourceQuery<T>, ForgeDbTempSourceQuery<T>> build)
    {
        if (build is null) throw new ArgumentNullException(nameof(build));
        _sourceSql = build(new ForgeDbTempSourceQuery<T>()).ToSql();
        return this;
    }

    public string ToSql()
    {
        var source = string.IsNullOrWhiteSpace(_sourceSql) ? $"SELECT * FROM {typeof(T).Name}" : _sourceSql;
        return $"SELECT * INTO {_name} FROM ({source}) AS ForgeTempSource";
    }

    public ValueTask<int> CreateAsync(CancellationToken cancellationToken = default)
        => _db.ExecuteAsync(ToSql(), cancellationToken: cancellationToken);
}

public sealed class ForgeDbTempSourceQuery<T>
{
    private string _table = typeof(T).Name;
    private readonly List<string> _where = new();

    public ForgeDbTempSourceQuery<T> From<TEntity>()
    {
        _table = typeof(TEntity).Name;
        return this;
    }

    public ForgeDbTempSourceQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeDbFluentExpressionSql.TranslateWhere(predicate));
        return this;
    }

    internal string ToSql()
    {
        var sql = new StringBuilder($"SELECT * FROM {_table}");
        if (_where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        return sql.ToString();
    }
}

internal static class ForgeDbFluentExpressionSql
{
    public static string TranslateWhere<T>(Expression<Func<T, bool>> expression) => Translate(expression.Body);

    private static string Translate(Expression expression)
    {
        if (expression is BinaryExpression b)
        {
            if (b.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var logical = b.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({Translate(b.Left)}) {logical} ({Translate(b.Right)})";
            }
            return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
        }
        throw new NotSupportedException($"Expression {expression.NodeType} is not supported by ForgeORM fluent CTE/temp SQL.");
    }

    private static string Member(Expression expression)
    {
        if (expression is UnaryExpression u) expression = u.Operand;
        if (expression is MemberExpression m && m.Expression?.NodeType == ExpressionType.Parameter)
            return m.Member.Name;
        throw new NotSupportedException("Left side must be an entity member.");
    }

    private static string Operator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        _ => throw new NotSupportedException($"Operator {type} is not supported.")
    };

    private static string Value(Expression expression)
    {
        var value = expression is ConstantExpression c ? c.Value : Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile().Invoke();
        return value switch
        {
            null => "NULL",
            string s => "'" + s.Replace("'", "''") + "'",
            DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss") + "'",
            DateTimeOffset d => "'" + d.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + "'",
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt64(e).ToString(System.Globalization.CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL",
            _ => "'" + value.ToString()!.Replace("'", "''") + "'"
        };
    }
}
