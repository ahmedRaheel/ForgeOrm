using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Fluent enterprise query builder with expression predicates, dynamic sorting, paging and SQL generation.
/// </summary>
public sealed class ForgeEnterpriseQuery<T>
{
    private readonly List<string> _columns = [];
    private readonly List<string> _where = [];
    private readonly List<ForgeSqlParameter> _parameters = [];
    private readonly ForgeExpressionSqlTranslator _translator = new();
    private string _table = typeof(T).Name;
    private string? _orderBy;
    private int? _skip;
    private int? _take;

    /// <summary>
    /// Sets the table name.
    /// </summary>
    public ForgeEnterpriseQuery<T> From(string table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Selects specific columns.
    /// </summary>
    public ForgeEnterpriseQuery<T> Select(params Expression<Func<T, object?>>[] columns)
    {
        foreach (var column in columns)
        {
            _columns.Add(GetMemberName(column));
        }

        return this;
    }

    /// <summary>
    /// Adds a predicate.
    /// </summary>
    public ForgeEnterpriseQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        var query = _translator.Translate(predicate);
        _where.Add(query.Sql);
        _parameters.AddRange(query.Parameters);
        return this;
    }

    /// <summary>
    /// Adds a conditional predicate.
    /// </summary>
    public ForgeEnterpriseQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? Where(predicate) : this;
    }

    /// <summary>
    /// Adds a BETWEEN predicate when both boundaries are supplied.
    /// </summary>
    public ForgeEnterpriseQuery<T> Between<TValue>(Expression<Func<T, TValue>> column, TValue? from, TValue? to)
    {
        var name = GetMemberName(column);
        if (from is not null)
        {
            var p = AddParameter(from);
            _where.Add($"{name} >= {p}");
        }

        if (to is not null)
        {
            var p = AddParameter(to);
            _where.Add($"{name} <= {p}");
        }

        return this;
    }

    /// <summary>
    /// Adds a LIKE contains predicate.
    /// </summary>
    public ForgeEnterpriseQuery<T> Contains(Expression<Func<T, string?>> column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        var name = GetMemberName(column);
        var p = AddParameter("%" + value.Trim() + "%");
        _where.Add($"{name} LIKE {p}");
        return this;
    }

    /// <summary>
    /// Adds strongly typed ordering.
    /// </summary>
    public ForgeEnterpriseQuery<T> OrderBy(Expression<Func<T, object?>> column, ForgeSortDirection direction = ForgeSortDirection.Ascending)
    {
        _orderBy = GetMemberName(column) + (direction == ForgeSortDirection.Descending ? " DESC" : " ASC");
        return this;
    }

    /// <summary>
    /// Adds dynamic ordering for grid screens.
    /// </summary>
    public ForgeEnterpriseQuery<T> OrderBy(string column, ForgeSortDirection direction = ForgeSortDirection.Ascending)
    {
        _orderBy = SanitizeIdentifier(column) + (direction == ForgeSortDirection.Descending ? " DESC" : " ASC");
        return this;
    }

    /// <summary>
    /// Applies page and page size.
    /// </summary>
    public ForgeEnterpriseQuery<T> Page(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 5000);
        _skip = (page - 1) * pageSize;
        _take = pageSize;
        return this;
    }

    /// <summary>
    /// Generates SQL for the selected provider dialect.
    /// </summary>
    public ForgeSqlQuery ToSql(ForgeQueryProviderDialect dialect = ForgeQueryProviderDialect.SqlServer)
    {
        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(_columns.Count == 0 ? "*" : string.Join(", ", _columns));
        sql.Append(" FROM ").Append(_table);

        if (_where.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        }

        if (!string.IsNullOrWhiteSpace(_orderBy))
        {
            sql.Append(" ORDER BY ").Append(_orderBy);
        }
        else if (_skip.HasValue || _take.HasValue)
        {
            sql.Append(" ORDER BY 1");
        }

        AppendPaging(sql, dialect);

        return new ForgeSqlQuery
        {
            Sql = sql.ToString(),
            Parameters = _parameters.ToArray()
        };
    }

    private void AppendPaging(StringBuilder sql, ForgeQueryProviderDialect dialect)
    {
        if (!_skip.HasValue && !_take.HasValue)
        {
            return;
        }

        var skip = _skip ?? 0;
        var take = _take ?? 50;

        switch (dialect)
        {
            case ForgeQueryProviderDialect.SqlServer:
                sql.Append($" OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY");
                break;
            case ForgeQueryProviderDialect.Oracle:
                sql.Append($" OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY");
                break;
            default:
                sql.Append($" LIMIT {take} OFFSET {skip}");
                break;
        }
    }

    private string AddParameter(object? value)
    {
        var name = $"@p{_parameters.Count}";
        _parameters.Add(new ForgeSqlParameter(name, value));
        return name;
    }

    private static string GetMemberName(LambdaExpression expression)
    {
        Expression body = expression.Body;
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new NotSupportedException("Only direct member expressions are supported.");
    }

    private static string SanitizeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Column name is required.", nameof(identifier));
        }

        if (identifier.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '_' or '.')))
        {
            throw new ArgumentException($"Unsafe column identifier '{identifier}'.", nameof(identifier));
        }

        return identifier;
    }
}
