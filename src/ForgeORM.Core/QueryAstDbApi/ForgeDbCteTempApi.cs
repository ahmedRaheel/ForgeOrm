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
