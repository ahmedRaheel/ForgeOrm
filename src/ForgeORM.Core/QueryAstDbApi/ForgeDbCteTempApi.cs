using System.Linq.Expressions;
using System.Text;
using ForgeORM.QueryAst;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>db.Select&lt;T&gt;() alias for ForgeSql.Select&lt;T&gt;(). Keeps the public API centered on db.</summary>
    public IForgeAstSelectBuilder<T> Select<T>() => ForgeSql.Select<T>();

    /// <summary>db.Script() alias for ForgeSql.Script().</summary>
    public IForgeAstScriptBuilder Script() => ForgeSql.Script();

    /// <summary>Creates a SQL-style CTE from db instead of ForgeSql.Cte.</summary>
    public ForgeCte Cte(string name, string sql) => ForgeSql.Cte(name, sql);

    /// <summary>Creates a CTE from a typed select builder.</summary>
    public ForgeCte Cte<T>(string name, Func<IForgeAstSelectBuilder<T>, IForgeAstSelectBuilder<T>> build)
    {
        if (build is null) throw new ArgumentNullException(nameof(build));
        var rendered = build(ForgeSql.Select<T>()).Render(Provider);
        return new ForgeCte(name, rendered.Sql);
    }

    /// <summary>Creates an expression-based CTE using selected columns and a where expression.</summary>
    public ForgeCte Cte<T>(
        string name,
        Expression<Func<T, bool>> where,
        params Expression<Func<T, object>>[] columns)
    {
        var table = ResolveCteTableName<T>();
        var projection = columns is { Length: > 0 }
            ? string.Join(", ", columns.Select(ForgeDbExpressionSql.MemberName))
            : "*";
        var filter = ForgeDbExpressionSql.TranslateWhere(where);
        return new ForgeCte(name, $"SELECT {projection} FROM {table} WHERE {filter}");
    }

    /// <summary>db.TempTable(name) alias for ForgeSql.TempTable(name).</summary>
    public IForgeAstTempTableBuilder TempTable(string name) => ForgeSql.TempTable(name);

    /// <summary>Creates a temp table using an imperative builder from db.</summary>
    public ForgeTempTable TempTable(string name, Action<IForgeAstTempTableBuilder> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var builder = ForgeSql.TempTable(name);
        configure(builder);
        return builder.Build();
    }

    /// <summary>
    /// Creates a temp table from typed property expressions. SQL types are inferred from CLR member types.
    /// Example: db.TempTable&lt;Order&gt;("#TopOrders", x => x.Id, x => x.CustomerId, x => x.GrandTotal)
    /// </summary>
    public ForgeTempTable TempTable<T>(string name, params Expression<Func<T, object>>[] columns)
    {
        var builder = ForgeSql.TempTable(name);
        foreach (var column in columns)
        {
            var memberType = ForgeDbExpressionSql.MemberType(column);
            builder.Column(ForgeDbExpressionSql.MemberName(column), ToDbType(memberType), Nullable.GetUnderlyingType(memberType) is not null || !memberType.IsValueType);
        }
        return builder.Build();
    }

    /// <summary>
    /// Creates a temp-table script from typed property expressions and inserts rows using an expression-based source query.
    /// </summary>
    public ForgeRenderedSql TempTableFrom<T>(
        string tempTableName,
        Expression<Func<T, bool>> where,
        params Expression<Func<T, object>>[] columns)
    {
        var temp = TempTable<T>(tempTableName, columns);
        var projection = columns is { Length: > 0 }
            ? string.Join(", ", columns.Select(ForgeDbExpressionSql.MemberName))
            : "*";
        var table = ResolveCteTableName<T>();
        var filter = ForgeDbExpressionSql.TranslateWhere(where);
        var source = $"SELECT {projection} FROM {table} WHERE {filter}";

        return ForgeSql.Script()
            .CreateTempTable(temp.Name, t =>
            {
                foreach (var c in temp.Columns)
                    t.Column(c.Name, c.DbType, c.Nullable);
                if (temp.PrimaryKeyColumns.Count > 0)
                    t.PrimaryKey(temp.PrimaryKeyColumns.ToArray());
            })
            .InsertIntoTemp(temp.Name, source)
            .Render(Provider);
    }

    private string ResolveCteTableName<T>()
    {
        try
        {
            var map = _metadata.Resolve<T>();
            if (!string.IsNullOrWhiteSpace(map.TableName)) return map.TableName;
        }
        catch
        {
            // Metadata resolution must not block expression SQL helpers in design-time/sample scenarios.
        }
        return typeof(T).Name;
    }

    private static string ToDbType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t == typeof(string)) return "NVARCHAR(MAX)";
        if (t == typeof(int)) return "INT";
        if (t == typeof(long)) return "BIGINT";
        if (t == typeof(short)) return "SMALLINT";
        if (t == typeof(byte)) return "TINYINT";
        if (t == typeof(bool)) return "BIT";
        if (t == typeof(decimal)) return "DECIMAL(18,2)";
        if (t == typeof(double)) return "FLOAT";
        if (t == typeof(float)) return "REAL";
        if (t == typeof(DateTime)) return "DATETIME2";
        if (t == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
        if (t == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (t.IsEnum) return "INT";
        return "NVARCHAR(MAX)";
    }
}

internal static class ForgeDbExpressionSql
{
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        var member = GetMemberExpression(expression.Body);
        return member.Member.Name;
    }

    public static Type MemberType<T>(Expression<Func<T, object>> expression)
    {
        var member = GetMemberExpression(expression.Body);
        return member.Type;
    }

    public static string TranslateWhere<T>(Expression<Func<T, bool>> expression)
        => Translate(expression.Body);

    private static string Translate(Expression expression)
    {
        if (expression is BinaryExpression binary)
        {
            if (binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var op = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({Translate(binary.Left)}) {op} ({Translate(binary.Right)})";
            }

            return $"{MemberSql(binary.Left)} {Operator(binary.NodeType)} {LiteralSql(binary.Right)}";
        }

        if (expression is MethodCallExpression call && call.Method.Name == nameof(string.Contains) && call.Object is not null)
            return $"{MemberSql(call.Object)} LIKE '%' + {LiteralSql(call.Arguments[0])} + '%'";

        throw new NotSupportedException($"Expression '{expression.NodeType}' is not supported for CTE/temp-table SQL generation yet.");
    }

    private static string MemberSql(Expression expression)
    {
        if (expression is UnaryExpression unary) expression = unary.Operand;
        if (expression is MemberExpression member && member.Expression?.NodeType == ExpressionType.Parameter)
            return member.Member.Name;
        throw new NotSupportedException("Left side must be an entity member expression.");
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

    private static string LiteralSql(Expression expression)
    {
        var value = Evaluate(expression);
        return value switch
        {
            null => "NULL",
            string s => "'" + s.Replace("'", "''") + "'",
            DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss") + "'",
            DateTimeOffset d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss zzz") + "'",
            Guid g => "'" + g + "'",
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt64(e).ToString(System.Globalization.CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL",
            _ => "'" + value.ToString()!.Replace("'", "''") + "'"
        };
    }

    private static object? Evaluate(Expression expression)
    {
        if (expression is ConstantExpression c) return c.Value;
        var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object)));
        return lambda.Compile().Invoke();
    }

    private static MemberExpression GetMemberExpression(Expression body)
    {
        if (body is UnaryExpression unary) body = unary.Operand;
        return body as MemberExpression
            ?? throw new NotSupportedException("Only simple member expressions are supported.");
    }
}
