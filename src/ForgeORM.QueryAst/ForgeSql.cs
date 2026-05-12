using System.Linq.Expressions;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

public static class ForgeSql
{
    public static IForgeAstSelectBuilder<T> Select<T>() => new ForgeAstSelectBuilder<T>();
    public static IForgeAstScriptBuilder Script() => new ForgeAstScriptBuilder();
    public static ForgeCte Cte(string name, string sql) => new(name, sql);
}

public interface IForgeAstSelectBuilder<T>
{
    IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns);
    IForgeAstSelectBuilder<T> From(string? tableName = null);
    IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate);
    IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null);
    IForgeAstSelectBuilder<T> InnerJoin(string table, string on);
    IForgeAstSelectBuilder<T> Join(string table, string on);
    IForgeAstSelectBuilder<T> LeftJoin(string table, string on);
    IForgeAstSelectBuilder<T> RightJoin(string table, string on);
    IForgeAstSelectBuilder<T> FullJoin(string table, string on);
    IForgeAstSelectBuilder<T> CrossJoin(string table);
    IForgeAstSelectBuilder<T> CrossApply(string tableExpression, string alias);
    IForgeAstSelectBuilder<T> OuterApply(string tableExpression, string alias);
    IForgeAstSelectBuilder<T> WithCte(string name, string sql);
    IForgeAstSelectBuilder<T> WithCte(ForgeCte cte);
    IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column);
    IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column);
    IForgeAstSelectBuilder<T> OrderBySql(string orderBy);
    IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns);
    IForgeAstSelectBuilder<T> HavingSql(string condition);
    IForgeAstSelectBuilder<T> Skip(int rows);
    IForgeAstSelectBuilder<T> Take(int rows);
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
}

public interface IForgeAstScriptBuilder
{
    IForgeAstScriptBuilder WithCte(string name, string sql);
    IForgeAstScriptBuilder CreateTempTable(string name, Action<IForgeAstTempTableBuilder> configure);
    IForgeAstScriptBuilder InsertIntoTemp(string tempTable, string selectSql);
    IForgeAstScriptBuilder Statement(string sql);
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
}

public interface IForgeAstTempTableBuilder
{
    IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true);
    IForgeAstTempTableBuilder PrimaryKey(params string[] columns);
    ForgeTempTable Build();
}

public sealed record ForgeRenderedSql(string Sql, object? Parameters = null);
public sealed record ForgeCte(string Name, string Sql);
public sealed record ForgeTempColumn(string Name, string DbType, bool Nullable);

public sealed class ForgeTempTable
{
    public required string Name { get; init; }
    public List<ForgeTempColumn> Columns { get; init; } = [];
    public List<string> PrimaryKeyColumns { get; init; } = [];
}

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

    public IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns) { _columns.AddRange(columns.Select(Ast.MemberName)); return this; }
    public IForgeAstSelectBuilder<T> From(string? tableName = null) { _table = tableName ?? ResolveTableName(typeof(T)); return this; }
    public IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate) { _where.Add(Ast.Translate(predicate)); return this; }
    public IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null) { _where.Add(condition); _parameters = parameters ?? _parameters; return this; }
    public IForgeAstSelectBuilder<T> Join(string table, string on) => InnerJoin(table, on);
    public IForgeAstSelectBuilder<T> InnerJoin(string table, string on) { _joins.Add($"INNER JOIN {table} ON {on}"); return this; }
    public IForgeAstSelectBuilder<T> LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    public IForgeAstSelectBuilder<T> RightJoin(string table, string on) { _joins.Add($"RIGHT JOIN {table} ON {on}"); return this; }
    public IForgeAstSelectBuilder<T> FullJoin(string table, string on) { _joins.Add($"FULL OUTER JOIN {table} ON {on}"); return this; }
    public IForgeAstSelectBuilder<T> CrossJoin(string table) { _joins.Add($"CROSS JOIN {table}"); return this; }
    public IForgeAstSelectBuilder<T> CrossApply(string tableExpression, string alias) { _joins.Add($"CROSS APPLY ({tableExpression}) {alias}"); return this; }
    public IForgeAstSelectBuilder<T> OuterApply(string tableExpression, string alias) { _joins.Add($"OUTER APPLY ({tableExpression}) {alias}"); return this; }
    public IForgeAstSelectBuilder<T> WithCte(string name, string sql) { _ctes.Add(new ForgeCte(name, sql)); return this; }
    public IForgeAstSelectBuilder<T> WithCte(ForgeCte cte) { _ctes.Add(cte); return this; }
    public IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column) { _orderBy = Ast.MemberName(column) + " ASC"; return this; }
    public IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column) { _orderBy = Ast.MemberName(column) + " DESC"; return this; }
    public IForgeAstSelectBuilder<T> OrderBySql(string orderBy) { _orderBy = orderBy; return this; }
    public IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns) { _groupBy.AddRange(columns.Select(Ast.MemberName)); return this; }
    public IForgeAstSelectBuilder<T> HavingSql(string condition) { _having = condition; return this; }
    public IForgeAstSelectBuilder<T> Skip(int rows) { _skip = rows; return this; }
    public IForgeAstSelectBuilder<T> Take(int rows) { _take = rows; return this; }

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

        sql.Append("SELECT ").Append(_columns.Count == 0 ? "*" : string.Join(", ", _columns));
        sql.Append(" FROM ").Append(_table);

        if (_joins.Count > 0) sql.Append(' ').Append(string.Join(" ", _joins));
        if (_where.Count > 0) sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        if (_groupBy.Count > 0) sql.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));
        if (!string.IsNullOrWhiteSpace(_having)) sql.Append(" HAVING ").Append(_having);
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY ").Append(_orderBy);

        if (_take.HasValue)
        {
            if (provider.ProviderName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ||
                provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY 1");
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
        var attr = type.GetCustomAttributes(typeof(ForgeTableAttribute), false).Cast<ForgeTableAttribute>().FirstOrDefault();
        return attr?.Name ?? type.Name;
    }
}

internal sealed class ForgeAstScriptBuilder : IForgeAstScriptBuilder
{
    private readonly List<ForgeCte> _ctes = [];
    private readonly List<ForgeTempTable> _tempTables = [];
    private readonly List<string> _statements = [];

    public IForgeAstScriptBuilder WithCte(string name, string sql) { _ctes.Add(new ForgeCte(name, sql)); return this; }
    public IForgeAstScriptBuilder CreateTempTable(string name, Action<IForgeAstTempTableBuilder> configure) { var b = new ForgeAstTempTableBuilder(name); configure(b); _tempTables.Add(b.Build()); return this; }
    public IForgeAstScriptBuilder InsertIntoTemp(string tempTable, string selectSql) { _statements.Add($"INSERT INTO {tempTable} {selectSql}"); return this; }
    public IForgeAstScriptBuilder Statement(string sql) { _statements.Add(sql); return this; }

    public ForgeRenderedSql Render(IForgeDatabaseProvider provider)
    {
        var sql = new StringBuilder();
        foreach (var temp in _tempTables) sql.AppendLine(RenderTempTable(provider, temp)).AppendLine();

        if (_ctes.Count > 0)
        {
            sql.Append("WITH ");
            sql.AppendLine(string.Join(", ", _ctes.Select(x => $"{x.Name} AS ({x.Sql})")));
        }

        foreach (var statement in _statements) sql.AppendLine(statement.TrimEnd(';') + ";");
        return new ForgeRenderedSql(sql.ToString());
    }

    private static string RenderTempTable(IForgeDatabaseProvider provider, ForgeTempTable table)
    {
        var tableName = provider.ProviderName.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) ? table.Name.TrimStart('#') : table.Name;
        var cols = table.Columns.Select(c => $"{c.Name} {c.DbType} {(c.Nullable ? "NULL" : "NOT NULL")}").ToList();
        if (table.PrimaryKeyColumns.Count > 0) cols.Add($"PRIMARY KEY ({string.Join(", ", table.PrimaryKeyColumns)})");
        if (provider.ProviderName.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)) return $"CREATE TEMP TABLE {tableName} ({string.Join(", ", cols)});";
        if (provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase)) return $"CREATE GLOBAL TEMPORARY TABLE {tableName.TrimStart('#')} ({string.Join(", ", cols)}) ON COMMIT PRESERVE ROWS;";
        return $"CREATE TABLE {tableName} ({string.Join(", ", cols)});";
    }
}

internal sealed class ForgeAstTempTableBuilder : IForgeAstTempTableBuilder
{
    private readonly ForgeTempTable _table;
    public ForgeAstTempTableBuilder(string name) { _table = new ForgeTempTable { Name = name }; }
    public IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true) { _table.Columns.Add(new ForgeTempColumn(name, dbType, nullable)); return this; }
    public IForgeAstTempTableBuilder PrimaryKey(params string[] columns) { _table.PrimaryKeyColumns.AddRange(columns); return this; }
    public ForgeTempTable Build() => _table;
}

internal static class Ast
{
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        Expression body = expression.Body is UnaryExpression u ? u.Operand : expression.Body;
        return body is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Only simple member expressions are supported.");
    }

    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        if (expression.Body is not BinaryExpression b) throw new NotSupportedException("Only simple binary expressions are supported.");
        return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
    }

    private static string Member(Expression e) => e is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Left side must be a member expression.");
    private static string Operator(ExpressionType t) => t switch { ExpressionType.Equal => "=", ExpressionType.NotEqual => "<>", ExpressionType.GreaterThan => ">", ExpressionType.GreaterThanOrEqual => ">=", ExpressionType.LessThan => "<", ExpressionType.LessThanOrEqual => "<=", _ => throw new NotSupportedException($"Operator {t} is not supported.") };
    private static string Value(Expression e)
    {
        var v = Expression.Lambda(e).Compile().DynamicInvoke();
        return v switch { null => "NULL", string s => $"'{s.Replace("'", "''")}'", DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'", bool b => b ? "1" : "0", _ => v?.ToString() ?? "NULL" };
    }
}
