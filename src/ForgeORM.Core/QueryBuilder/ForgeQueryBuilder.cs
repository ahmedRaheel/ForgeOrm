using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>
/// Entry-point helper for lambda-based saved query registration.
/// </summary>
public sealed class ForgeSavedQueryBuilderRoot
{
    private readonly ForgeDb _db;

    internal ForgeSavedQueryBuilderRoot(ForgeDb db)
    {
        _db = db;
    }

    internal object? CurrentBuilder { get; private set; }

    /// <summary>
    /// Starts a typed query from the mapped table for <typeparamref name="TEntity"/>.
    /// </summary>
    public ForgeQueryBuilder<TEntity> From<TEntity>()
        where TEntity : class, new()
    {
        var builder = new ForgeQueryBuilder<TEntity>(_db).From<TEntity>();
        CurrentBuilder = builder;
        return builder;
    }

    internal ForgeRenderedQuery Render()
    {
        if (CurrentBuilder is null)
        {
            throw new InvalidOperationException("Saved query registration must call query.From<TEntity>() before rendering.");
        }

        var render = CurrentBuilder.GetType().GetMethod(nameof(ForgeQueryBuilder<object>.Render));
        if (render is null)
        {
            throw new InvalidOperationException("Saved query builder does not expose Render().");
        }

        return (ForgeRenderedQuery)render.Invoke(CurrentBuilder, null)!;
    }
}

/// <summary>
/// Lightweight expression-based query builder used by SavedQueries and samples.
/// </summary>
public sealed class ForgeQueryBuilder<TEntity>
    where TEntity : class, new()
{
    private readonly ForgeDb _db;
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private int _parameterIndex;

    internal ForgeQueryBuilder(ForgeDb db)
    {
        _db = db;
        TableName = ForgeEntityShape.ResolveTableName(typeof(TEntity));
    }

    internal string TableName { get; private set; }
    internal List<string> SelectColumns { get; } = [];
    internal List<string> WhereClauses { get; } = [];
    internal List<string> OrderClauses { get; } = [];
    internal int? SkipCount { get; private set; }
    internal int? TakeCount { get; private set; }

    /// <summary>Uses the mapped table for the supplied entity type.</summary>
    public ForgeQueryBuilder<TEntity> From<TTable>()
    {
        TableName = ForgeEntityShape.ResolveTableName(typeof(TTable));
        return this;
    }

    /// <summary>Uses an explicit table or view name.</summary>
    public ForgeQueryBuilder<TEntity> From(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name is required.", nameof(tableName));
        }

        TableName = tableName;
        return this;
    }

    /// <summary>Selects all columns.</summary>
    public ForgeQueryBuilder<TEntity> SelectAll()
    {
        SelectColumns.Clear();
        return this;
    }

    /// <summary>Selects columns by expression.</summary>
    public ForgeQueryBuilder<TEntity> Select(params Expression<Func<TEntity, object?>>[] columns)
    {
        foreach (var column in columns)
        {
            SelectColumns.Add(ForgeEntityShape.ColumnName(GetProperty(column.Body)));
        }

        return this;
    }

    /// <summary>Adds a typed where condition.</summary>
    public ForgeQueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        WhereClauses.Add(ForgeExpressionSqlBuilder.Build(predicate.Body, AddParameter));
        return this;
    }

    /// <summary>Adds raw SQL where condition.</summary>
    public ForgeQueryBuilder<TEntity> WhereSql(string sql, object? parameters = null)
    {
        if (!string.IsNullOrWhiteSpace(sql))
        {
            WhereClauses.Add(sql);
            MergeObjectParameters(parameters);
        }

        return this;
    }

    /// <summary>Adds an ascending order clause.</summary>
    public ForgeQueryBuilder<TEntity> OrderBy<TValue>(Expression<Func<TEntity, TValue>> expression)
    {
        var property = GetProperty(expression.Body);
        OrderClauses.Add($"{ForgeEntityShape.ColumnName(property)} ASC");
        return this;
    }

    /// <summary>Adds a descending order clause.</summary>
    public ForgeQueryBuilder<TEntity> OrderByDescending<TValue>(Expression<Func<TEntity, TValue>> expression)
    {
        var property = GetProperty(expression.Body);
        OrderClauses.Add($"{ForgeEntityShape.ColumnName(property)} DESC");
        return this;
    }

    /// <summary>Skips rows.</summary>
    public ForgeQueryBuilder<TEntity> Skip(int count)
    {
        SkipCount = Math.Max(0, count);
        return this;
    }

    /// <summary>Takes rows.</summary>
    public ForgeQueryBuilder<TEntity> Take(int count)
    {
        TakeCount = Math.Max(0, count);
        return this;
    }

    /// <summary>Applies paging.</summary>
    public ForgeQueryBuilder<TEntity> Page(int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safeSize = Math.Max(1, pageSize);
        SkipCount = (safePage - 1) * safeSize;
        TakeCount = safeSize;
        return this;
    }

    /// <summary>Renders SQL and parameters.</summary>
    public ForgeRenderedQuery Render()
    {
        var select = SelectColumns.Count == 0 ? "*" : string.Join(", ", SelectColumns);
        var sql = $"SELECT {select} FROM {TableName}";

        if (WhereClauses.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", WhereClauses);
        }

        if (OrderClauses.Count > 0)
        {
            sql += " ORDER BY " + string.Join(", ", OrderClauses);
        }
        else if (SkipCount.HasValue || TakeCount.HasValue)
        {
            sql += " ORDER BY (SELECT 1)";
        }

        if (SkipCount.HasValue || TakeCount.HasValue)
        {
            sql += $" OFFSET {SkipCount.GetValueOrDefault()} ROWS";
            if (TakeCount.HasValue)
            {
                sql += $" FETCH NEXT {TakeCount.Value} ROWS ONLY";
            }
        }

        return new ForgeRenderedQuery(sql, new Dictionary<string, object?>(_parameters, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Renders SQL text.</summary>
    public string ToSql() => Render().Sql;

    /// <summary>Executes the query.</summary>
    public Task<IReadOnlyList<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var query = Render();
        return _db.QueryAsync<TEntity>(query.Sql, query.Parameters, cancellationToken: cancellationToken);
    }

    private string AddParameter(object? value)
    {
        var name = $"p{_parameterIndex++}";
        _parameters[name] = value;
        return "@" + name;
    }

    private void MergeObjectParameters(object? parameters)
    {
        if (parameters is null) return;

        foreach (var prop in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
        {
            _parameters[prop.Name] = prop.GetValue(parameters);
        }
    }

    private static PropertyInfo GetProperty(Expression expression)
    {
        while (expression is UnaryExpression unary &&
               (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked || unary.NodeType == ExpressionType.TypeAs))
        {
            expression = unary.Operand;
        }

        if (expression is MemberExpression { Member: PropertyInfo property })
        {
            return property;
        }

        throw new NotSupportedException("Expression must point to a property.");
    }
}

/// <summary>Rendered SQL plus parameters.</summary>
public sealed record ForgeRenderedQuery(string Sql, IReadOnlyDictionary<string, object?> Parameters);

internal static class ForgeExpressionSqlBuilder
{
    public static string Build(Expression expression, Func<object?, string> addParameter)
    {
        if (expression is BinaryExpression binary)
        {
            var left = Build(binary.Left, addParameter);
            var right = Build(binary.Right, addParameter);

            return binary.NodeType switch
            {
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",
                ExpressionType.AndAlso => $"({left} AND {right})",
                ExpressionType.OrElse => $"({left} OR {right})",
                _ => throw new NotSupportedException($"Expression '{binary.NodeType}' is not supported.")
            };
        }

        if (expression is MemberExpression member)
        {
            if (member.Expression?.NodeType == ExpressionType.Parameter && member.Member is PropertyInfo property)
            {
                return ForgeEntityShape.ColumnName(property);
            }

            return addParameter(Evaluate(expression));
        }

        if (expression is ConstantExpression constant)
        {
            return addParameter(constant.Value);
        }

        if (expression is UnaryExpression unary &&
            (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            return Build(unary.Operand, addParameter);
        }

        throw new NotSupportedException($"Expression '{expression.NodeType}' is not supported.");
    }

    private static object? Evaluate(Expression expression)
    {
        var converted = Expression.Convert(expression, typeof(object));
        return Expression.Lambda<Func<object?>>(converted).Compile().Invoke();
    }
}
