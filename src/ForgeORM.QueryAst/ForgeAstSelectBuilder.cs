using System.Linq.Expressions;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

internal sealed class ForgeAstSelectBuilder<T> : IForgeAstSelectBuilder<T>
{
    private readonly List<string> _columns = [];
    private readonly List<string> _joins = [];
    private readonly List<string> _where = [];
    private readonly List<string> _groupBy = [];
    private readonly List<ForgeCte> _ctes = [];
    private string? _table;
    private string? _orderBy;
    private string? _having;
    private int? _skip;
    private int? _take;
    private object? _parameters;

    public IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns)
    {
        _columns.AddRange(columns.Select(ForgeAstExpression.MemberName));
        return this;
    }

    public IForgeAstSelectBuilder<T> From(string? tableName = null)
    {
        _table = tableName ?? ResolveTableName(typeof(T));
        return this;
    }

    public IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeAstExpression.Translate(predicate));
        return this;
    }

    public IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null)
    {
        _where.Add(condition);
        _parameters = parameters ?? _parameters;
        return this;
    }

    public IForgeAstSelectBuilder<T> Join(string table, string on)
    {
        _joins.Add($"INNER JOIN {table} ON {on}");
        return this;
    }

    public IForgeAstSelectBuilder<T> LeftJoin(string table, string on)
    {
        _joins.Add($"LEFT JOIN {table} ON {on}");
        return this;
    }

    public IForgeAstSelectBuilder<T> WithCte(string name, string sql)
    {
        _ctes.Add(new ForgeCte(name, sql));
        return this;
    }

    public IForgeAstSelectBuilder<T> WithCte(ForgeCte cte)
    {
        _ctes.Add(cte);
        return this;
    }

    public IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column)
    {
        _orderBy = ForgeAstExpression.MemberName(column) + " ASC";
        return this;
    }

    public IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column)
    {
        _orderBy = ForgeAstExpression.MemberName(column) + " DESC";
        return this;
    }

    public IForgeAstSelectBuilder<T> OrderBySql(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    public IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns)
    {
        _groupBy.AddRange(columns.Select(ForgeAstExpression.MemberName));
        return this;
    }

    public IForgeAstSelectBuilder<T> HavingSql(string condition)
    {
        _having = condition;
        return this;
    }

    public IForgeAstSelectBuilder<T> Skip(int rows)
    {
        _skip = rows;
        return this;
    }

    public IForgeAstSelectBuilder<T> Take(int rows)
    {
        _take = rows;
        return this;
    }

    public ForgeRenderedSql Render(IForgeDatabaseProvider provider)
    {
        _table ??= ResolveTableName(typeof(T));

        var sql = new StringBuilder();

        if (_ctes.Count > 0)
        {
            sql.Append("WITH ");
            sql.Append(string.Join(", ", _ctes.Select(x => $"{x.Name} AS ({x.Sql})")));
            sql.AppendLine();
        }

        sql.Append("SELECT ");
        sql.Append(_columns.Count == 0 ? "*" : string.Join(", ", _columns));
        sql.Append(" FROM ");
        sql.Append(_table);

        if (_joins.Count > 0)
            sql.Append(' ').Append(string.Join(" ", _joins));

        if (_where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));

        if (_groupBy.Count > 0)
            sql.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));

        if (!string.IsNullOrWhiteSpace(_having))
            sql.Append(" HAVING ").Append(_having);

        if (!string.IsNullOrWhiteSpace(_orderBy))
            sql.Append(" ORDER BY ").Append(_orderBy);

        if (_take.HasValue)
        {
            if (provider.ProviderName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ||
                provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_orderBy))
                    sql.Append(" ORDER BY 1");

                sql.Append($" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take.Value} ROWS ONLY");
            }
            else
            {
                sql.Append($" LIMIT {_take.Value} OFFSET {_skip ?? 0}");
            }
        }

        return new ForgeRenderedSql(sql.ToString(), _parameters);
    }

    private static string ResolveTableName(Type type)
    {
        var attr = type
            .GetCustomAttributes(typeof(ForgeTableAttribute), false)
            .Cast<ForgeTableAttribute>()
            .FirstOrDefault();

        return attr?.Name ?? type.Name;
    }
}
