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
    internal string? ProfileName { get; private set; }
    internal TimeSpan? CacheDuration { get; private set; }
    internal int? TimeoutSeconds { get; private set; }
    internal string? QueryTag { get; private set; }
    internal string? QueryComment { get; private set; }
    internal List<string> Joins { get; } = [];


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


    /// <summary>Adds raw SELECT projection text for advanced SQL scenarios.</summary>
    public ForgeQueryBuilder<TEntity> SelectSql(params string[] columns)
    {
        foreach (var column in columns.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            SelectColumns.Add(column);
        }

        return this;
    }

    /// <summary>Adds an INNER JOIN clause.</summary>
    public ForgeQueryBuilder<TEntity> InnerJoin(string table, string on)
    {
        Joins.Add($"INNER JOIN {table} ON {on}");
        return this;
    }

    /// <summary>Adds a LEFT JOIN clause.</summary>
    public ForgeQueryBuilder<TEntity> LeftJoin(string table, string on)
    {
        Joins.Add($"LEFT JOIN {table} ON {on}");
        return this;
    }

    /// <summary>Adds a CROSS APPLY / LATERAL style clause for SQL Server.</summary>
    public ForgeQueryBuilder<TEntity> CrossApply(string sql, string alias)
    {
        Joins.Add($"CROSS APPLY ({sql}) {alias}");
        return this;
    }

    /// <summary>Adds an EXISTS predicate.</summary>
    public ForgeQueryBuilder<TEntity> WhereExists(string subQuerySql, object? parameters = null)
    {
        WhereClauses.Add($"EXISTS ({subQuerySql})");
        MergeObjectParameters(parameters);
        return this;
    }

    /// <summary>Adds a NOT EXISTS predicate.</summary>
    public ForgeQueryBuilder<TEntity> WhereNotExists(string subQuerySql, object? parameters = null)
    {
        WhereClauses.Add($"NOT EXISTS ({subQuerySql})");
        MergeObjectParameters(parameters);
        return this;
    }

    /// <summary>Adds a CASE WHEN projection.</summary>
    public ForgeQueryBuilder<TEntity> CaseWhen(string conditionSql, string whenTrueSql, string whenFalseSql, string alias)
    {
        SelectColumns.Add($"CASE WHEN {conditionSql} THEN {whenTrueSql} ELSE {whenFalseSql} END AS {alias}");
        return this;
    }

    /// <summary>Adds JSON_VALUE comparison for JSON columns.</summary>
    public ForgeQueryBuilder<TEntity> WhereJsonValue(string jsonColumn, string jsonPath, string op, object? value)
    {
        var parameter = AddParameter(value);
        WhereClauses.Add($"JSON_VALUE({jsonColumn}, '{jsonPath}') {op} {parameter}");
        return this;
    }

    /// <summary>Adds SQL Server CONTAINS full-text predicate.</summary>
    public ForgeQueryBuilder<TEntity> WhereFullText(string column, string search)
    {
        var parameter = AddParameter(search);
        WhereClauses.Add($"CONTAINS({column}, {parameter})");
        return this;
    }

    /// <summary>Targets a SQL Server temporal table snapshot.</summary>
    public ForgeQueryBuilder<TEntity> ForSystemTimeAsOf(DateTimeOffset asOf)
    {
        TableName = $"{TableName} FOR SYSTEM_TIME AS OF {AddParameter(asOf)}";
        return this;
    }

    /// <summary>Adds profiling to the query execution.</summary>
    public ForgeQueryBuilder<TEntity> Profile(string name)
    {
        ProfileName = name;
        return this;
    }

    /// <summary>Adds query tag text as a SQL comment.</summary>
    public ForgeQueryBuilder<TEntity> Tag(string tag)
    {
        QueryTag = tag;
        return this;
    }

    /// <summary>Adds query comment text as a SQL comment.</summary>
    public ForgeQueryBuilder<TEntity> Comment(string comment)
    {
        QueryComment = comment;
        return this;
    }

    /// <summary>Configures command timeout seconds.</summary>
    public ForgeQueryBuilder<TEntity> Timeout(int seconds)
    {
        TimeoutSeconds = seconds;
        return this;
    }

    /// <summary>Configures in-memory query cache duration placeholder.</summary>
    public ForgeQueryBuilder<TEntity> CacheFor(TimeSpan duration)
    {
        CacheDuration = duration;
        return this;
    }


    /// <summary>
    /// Adds SQL Server NOLOCK hint for read-heavy dashboards where dirty reads are acceptable.
    /// Prefer SnapshotRead for financial/transactional screens.
    /// </summary>
    public ForgeQueryBuilder<TEntity> NoLock()
    {
        return WithTableHint("NOLOCK");
    }

    /// <summary>
    /// Adds SQL Server UPDLOCK hint for pessimistic update workflows.
    /// </summary>
    public ForgeQueryBuilder<TEntity> UpdateLock()
    {
        return WithTableHint("UPDLOCK");
    }

    /// <summary>
    /// Adds SQL Server READPAST hint to skip locked rows for queue/worker scenarios.
    /// </summary>
    public ForgeQueryBuilder<TEntity> ReadPast()
    {
        return WithTableHint("READPAST");
    }

    /// <summary>
    /// Adds SQL Server ROWLOCK hint to prefer row-level locks.
    /// </summary>
    public ForgeQueryBuilder<TEntity> RowLock()
    {
        return WithTableHint("ROWLOCK");
    }

    /// <summary>
    /// Uses a safe comment marker indicating snapshot-read intent.
    /// Enable READ_COMMITTED_SNAPSHOT or SNAPSHOT isolation at database level for true snapshot behavior.
    /// </summary>
    public ForgeQueryBuilder<TEntity> SnapshotRead()
    {
        QueryComment = string.IsNullOrWhiteSpace(QueryComment)
            ? "ForgeORM SnapshotRead requested"
            : QueryComment + " | ForgeORM SnapshotRead requested";

        return this;
    }

    /// <summary>
    /// Applies provider-specific read consistency metadata to the generated SQL comment.
    /// </summary>
    public ForgeQueryBuilder<TEntity> WithReadConsistency(ForgeReadConsistency consistency)
    {
        return consistency switch
        {
            ForgeReadConsistency.Default => this,
            ForgeReadConsistency.NoLock => NoLock(),
            ForgeReadConsistency.UpdateLock => UpdateLock(),
            ForgeReadConsistency.ReadPast => ReadPast(),
            ForgeReadConsistency.RowLock => RowLock(),
            ForgeReadConsistency.Snapshot => SnapshotRead(),
            _ => this
        };
    }

    private ForgeQueryBuilder<TEntity> WithTableHint(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return this;
        }

        var hintText = $"WITH ({hint})";

        if (TableName.Contains(" WITH ", StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        TableName = $"{TableName} {hintText}";
        return this;
    }


    /// <summary>Validates the rendered SQL for dangerous patterns.</summary>
    public ForgeQueryValidationResult Validate()
    {
        var sql = ToSql();
        var warnings = new List<string>();
        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)) warnings.Add("SELECT * is convenient but avoid it on large enterprise screens.");
        if (!sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)) warnings.Add("Query has no WHERE clause.");
        if ((SkipCount.HasValue || TakeCount.HasValue) && OrderClauses.Count == 0) warnings.Add("Paging without deterministic ORDER BY can return inconsistent pages.");
        return new ForgeQueryValidationResult(warnings.Count == 0, warnings);
    }

    /// <summary>Returns SQL Server EXPLAIN equivalent using estimated execution plan.</summary>
    public string Explain()
    {
        return "SET SHOWPLAN_XML ON;\n" + ToSql() + ";\nSET SHOWPLAN_XML OFF;";
    }

    /// <summary>Returns provider-neutral debug SQL and parameters.</summary>
    public ForgeDebugSql ToDebugSql()
    {
        var rendered = Render();
        return new ForgeDebugSql(rendered.Sql, rendered.Parameters);
    }

    /// <summary>Analyzes the query and returns index/query suggestions.</summary>
    public ForgeQueryBuilderAnalysis Analyze()
    {
        var rendered = Render();
        return ForgeQueryBuilderIndexSuggestionEngine.Analyze<TEntity>(rendered, WhereClauses, OrderClauses, TableName);
    }

    /// <summary>Creates a shallow clone of the query builder.</summary>
    public ForgeQueryBuilder<TEntity> Clone()
    {
        var clone = new ForgeQueryBuilder<TEntity>(_db).From(TableName);
        clone.SelectColumns.AddRange(SelectColumns);
        clone.WhereClauses.AddRange(WhereClauses);
        clone.OrderClauses.AddRange(OrderClauses);
        clone.Joins.AddRange(Joins);
        clone.SkipCount = SkipCount;
        clone.TakeCount = TakeCount;
        clone.ProfileName = ProfileName;
        clone.CacheDuration = CacheDuration;
        clone.TimeoutSeconds = TimeoutSeconds;
        clone.QueryTag = QueryTag;
        clone.QueryComment = QueryComment;
        foreach (var pair in _parameters) clone._parameters[pair.Key] = pair.Value;
        clone._parameterIndex = _parameterIndex;
        return clone;
    }

    /// <summary>Renders SQL and parameters.</summary>
    public ForgeRenderedQuery Render()
    {
        var select = SelectColumns.Count == 0 ? "*" : string.Join(", ", SelectColumns);
        var prefix = string.Empty;
        if (!string.IsNullOrWhiteSpace(QueryTag)) prefix += $"/* Tag: {QueryTag} */\n";
        if (!string.IsNullOrWhiteSpace(QueryComment)) prefix += $"/* {QueryComment} */\n";
        var sql = $"{prefix}SELECT {select} FROM {TableName}";

        if (Joins.Count > 0)
        {
            sql += " " + string.Join(" ", Joins);
        }

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
            sql += " ORDER BY 1";
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
    public async Task<IReadOnlyList<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var query = Render();
        var rows = await ForgeCompiledQueryCache.GetOrExecuteAsync(
            query.Sql,
            query.Parameters,
            CacheDuration,
            () => _db.QueryAsync<TEntity>(query.Sql, query.Parameters, TimeoutSeconds, cancellationToken),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(ProfileName))
        {
            ForgeQueryBuilderProfiler.Record(new ForgeQueryBuilderProfileEntry(
                ProfileName!,
                query.Sql,
                started,
                DateTimeOffset.UtcNow - started,
                rows.Count));
        }

        return rows;
    }

    /// <summary>Streams query rows as an async enumerable. The default implementation materializes then yields for provider safety.</summary>
    public async IAsyncEnumerable<TEntity> StreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rows = await ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
        }
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
            _parameters[prop.Name] = ForgeRuntimeAccessorCache.Get(prop, parameters);
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
        return ForgeExpressionDelegateCache.Evaluate(converted);
    }
}


/// <summary>Query validation result.</summary>
public sealed record ForgeQueryValidationResult(bool IsValid, IReadOnlyList<string> Warnings);

/// <summary>Debug SQL representation.</summary>
public sealed record ForgeDebugSql(string Sql, IReadOnlyDictionary<string, object?> Parameters);

/// <summary>Profile entry for query execution.</summary>
public sealed record ForgeQueryBuilderProfileEntry(string Name, string Sql, DateTimeOffset StartedAtUtc, TimeSpan Duration, int Rows);

/// <summary>Query analysis output with suggested indexes.</summary>
public sealed record ForgeQueryBuilderAnalysis(string Entity, string Sql, IReadOnlyList<string> SuggestedIndexes, IReadOnlyList<string> Notes);

/// <summary>In-memory query profiler for diagnostics and samples.</summary>
public static class ForgeQueryBuilderProfiler
{
    private static readonly List<ForgeQueryBuilderProfileEntry> Entries = [];

    public static void Record(ForgeQueryBuilderProfileEntry entry)
    {
        lock (Entries) Entries.Add(entry);
    }

    public static IReadOnlyList<ForgeQueryBuilderProfileEntry> Snapshot()
    {
        lock (Entries) return Entries.ToList();
    }

    public static void Clear()
    {
        lock (Entries) Entries.Clear();
    }
}

/// <summary>Simple index advisor using rendered query metadata.</summary>
public static class ForgeQueryBuilderIndexSuggestionEngine
{
    public static ForgeQueryBuilderAnalysis Analyze<TEntity>(ForgeRenderedQuery query, IReadOnlyList<string> whereClauses, IReadOnlyList<string> orderClauses, string tableName)
    {
        var table = tableName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? typeof(TEntity).Name;
        var clean = table.Replace("dbo.", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("[", string.Empty).Replace("]", string.Empty);
        var suggestions = new List<string>();
        if (whereClauses.Count > 0 || orderClauses.Count > 0)
        {
            suggestions.Add($"CREATE INDEX IX_{clean}_Query ON {table} (/* WHERE columns first */) INCLUDE (/* projected columns */);");
        }
        if (query.Sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Place ORDER BY columns after equality-filter columns in composite indexes.");
        }
        if (query.Sql.Contains("LIKE", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("For leading-wildcard LIKE searches, consider full-text search or trigram indexes.");
        }
        return new ForgeQueryBuilderAnalysis(typeof(TEntity).Name, query.Sql, suggestions, ["Use actual execution plans before creating indexes in production."]);
    }
}

/// <summary>Small in-memory compiled/rendered query cache foundation.</summary>
public static class ForgeCompiledQueryCache
{
    private static readonly Dictionary<string, (DateTimeOffset Expires, object Rows)> Cache = new(StringComparer.Ordinal);

    public static async Task<IReadOnlyList<T>> GetOrExecuteAsync<T>(string sql, IReadOnlyDictionary<string, object?> parameters, TimeSpan? duration, Func<Task<IReadOnlyList<T>>> factory, CancellationToken cancellationToken)
    {
        if (duration is null || duration.Value <= TimeSpan.Zero)
        {
            return await factory();
        }

        var key = sql + "|" + string.Join(";", parameters.OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Value));
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var hit) && hit.Expires > DateTimeOffset.UtcNow && hit.Rows is IReadOnlyList<T> typed)
            {
                return typed;
            }
        }

        var rows = await factory();
        lock (Cache) Cache[key] = (DateTimeOffset.UtcNow.Add(duration.Value), rows);
        return rows;
    }
}
