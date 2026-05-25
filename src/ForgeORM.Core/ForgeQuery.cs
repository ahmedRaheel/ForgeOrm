using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed class ForgeQuery<T> : IForgeQuery<T>
{
    private readonly IForgeDb _db;
    private readonly ForgeEntityMetadata _meta;
    private readonly string? _baseSql;
    private readonly object? _baseParameters;
    private static readonly ConcurrentDictionary<string, string> QuerySqlCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string> CountSqlCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string> AnySqlCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string> AggregateSqlCache = new(StringComparer.Ordinal);

    private readonly List<string> _where = [];
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private string? _orderBy;
    private int? _skip;
    private int? _take;
    private readonly List<PropertyInfo> _includes = [];
    private string? _temporalClause;
    private readonly Dictionary<string, object?> _temporalParameters = new(StringComparer.OrdinalIgnoreCase);

    public ForgeQueryExecutionOptions ExecutionOptions { get; } = new();

    /// <summary>
    /// Executes the ForgeQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="meta">The meta value.</param>
    /// <param name="baseSql">The baseSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the ForgeQuery operation.</returns>
    public ForgeQuery(IForgeDb db, ForgeEntityMetadata meta, string? baseSql = null, object? parameters = null)
    {
        _db = db;
        _meta = meta;
        _baseSql = baseSql;
        _baseParameters = parameters;
        MergeParameters(parameters);
    }

    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Where operation.</returns>
    public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeExpressionTranslator.Translate(predicate));
        return this;
    }

    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    public IForgeQuery<T> Where(string condition, object? parameters = null) => WhereSql(condition, parameters);

    /// <summary>
    /// Executes the WhereSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    public IForgeQuery<T> WhereSql(string condition, object? parameters = null)
    {
        _where.Add(condition);
        MergeParameters(parameters);
        return this;
    }

    /// <summary>
    /// Executes the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the WhereIf operation.</returns>
    public IForgeQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
        => condition ? Where(predicate) : this;

    /// <summary>
    /// Executes the WhereSqlIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="sqlCondition">The sqlCondition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSqlIf operation.</returns>
    public IForgeQuery<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null)
        => condition ? WhereSql(sqlCondition, parameters) : this;

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="keySelector">The keySelector value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector)
    {
        _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " ASC";
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="keySelector">The keySelector value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector)
    {
        _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " DESC";
        return this;
    }

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public IForgeQuery<T> OrderBy(string orderBy) => OrderBySql(orderBy);

    /// <summary>
    /// Executes the OrderBySql operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    public IForgeQuery<T> OrderBySql(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    /// <summary>
    /// Executes the Skip operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Skip operation.</returns>
    public IForgeQuery<T> Skip(int count)
    {
        _skip = count;
        return this;
    }

    /// <summary>
    /// Executes the Take operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Take operation.</returns>
    public IForgeQuery<T> Take(int count)
    {
        _take = count;
        return this;
    }


    public IForgeQuery<T> TemporalAll()
    {
        _temporalClause = "FOR SYSTEM_TIME ALL";
        return this;
    }

    public IForgeQuery<T> TemporalAsOf(DateTime asOfUtc)
    {
        _temporalClause = "FOR SYSTEM_TIME AS OF @TemporalAsOf";
        _temporalParameters["TemporalAsOf"] = asOfUtc;
        return this;
    }

    public IForgeQuery<T> TemporalBetween(DateTime fromUtc, DateTime toUtc)
    {
        _temporalClause = "FOR SYSTEM_TIME BETWEEN @TemporalFrom AND @TemporalTo";
        _temporalParameters["TemporalFrom"] = fromUtc;
        _temporalParameters["TemporalTo"] = toUtc;
        return this;
    }

    public IForgeQuery<T> TemporalContainedIn(DateTime fromUtc, DateTime toUtc)
    {
        _temporalClause = "FOR SYSTEM_TIME CONTAINED IN (@TemporalFrom, @TemporalTo)";
        _temporalParameters["TemporalFrom"] = fromUtc;
        _temporalParameters["TemporalTo"] = toUtc;
        return this;
    }

    /// <summary>
    /// Includes a reference or collection navigation. The navigation is loaded with split query only when a terminal entity method executes.
    /// </summary>
    public IForgeQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> navigation)
    {
        if (navigation.Body is not MemberExpression memberExpression)
            throw new NotSupportedException("Only direct navigation includes are supported. Example: Include(x => x.Items).");

        if (memberExpression.Member is not PropertyInfo property)
            throw new NotSupportedException("Include must target a property.");

        if (!ForgeNavigationSupport.IsCollectionNavigation(property) && !ForgeNavigationSupport.IsReferenceNavigation(property))
            throw new InvalidOperationException($"Property '{property.Name}' is not a navigation property.");

        if (_includes.All(x => !x.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
            _includes.Add(property);

        return this;
    }

    /// <summary>
    /// Executes the Any operation.
    /// </summary>
    /// <returns>The result of the Any operation.</returns>
    public bool Any() => _db.ExecuteScalar<int>(BuildAnySql(), BuildParameters(), ExecutionOptions.TimeoutSeconds) > 0;

    /// <summary>
    /// Executes the AnyAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AnyAsync operation.</returns>
    public async ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await _db.ExecuteScalarAsync<int>(BuildAnySql(), BuildParameters(), cancellationToken: cancellationToken) > 0;

    /// <summary>
    /// Executes the ToList operation.
    /// </summary>
    /// <returns>The result of the ToList operation.</returns>
    public IReadOnlyList<T> ToList()
    {
        var sql = BuildSql();
        var parameters = BuildParameters();
        var cacheKey = ForgeSecondLevelQueryCache.BuildKey(typeof(T), sql, parameters, _includes, ExecutionOptions);
        if (ForgeSecondLevelQueryCache.TryGetList<T>(this, cacheKey, out var cachedRows))
            return cachedRows;

        var rows = _db.Query<T>(sql, parameters, ExecutionOptions.TimeoutSeconds);
        if (_includes.Count > 0 && rows.Count > 0)
        {
            var plan = ForgeCompiledIncludePlanCache.GetOrCreate<T>(_includes, ForgeEfStyleSplitQueryExtensions.GetEfSplitOptions(this));
            ForgeEfSplitGraphLoader.LoadIncludedNavigationsAsync(rows, _db, plan.Includes, CancellationToken.None).GetAwaiter().GetResult();
        }

        ForgeSecondLevelQueryCache.SetList(this, cacheKey, rows);
        return rows;
    }

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public async ValueTask<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var sql = BuildSql();
        var parameters = BuildParameters();
        var cacheKey = ForgeSecondLevelQueryCache.BuildKey(typeof(T), sql, parameters, _includes, ExecutionOptions);
        if (ForgeSecondLevelQueryCache.TryGetList<T>(this, cacheKey, out var cachedRows))
            return cachedRows;

        var rows = await _db.QueryAsync<T>(sql, parameters, ExecutionOptions.TimeoutSeconds, cancellationToken).ConfigureAwait(false);
        if (_includes.Count > 0 && rows.Count > 0)
        {
            var plan = ForgeCompiledIncludePlanCache.GetOrCreate<T>(_includes, ForgeEfStyleSplitQueryExtensions.GetEfSplitOptions(this));
            await ForgeEfSplitGraphLoader.LoadIncludedNavigationsAsync(rows, _db, plan.Includes, cancellationToken).ConfigureAwait(false);
        }

        ForgeSecondLevelQueryCache.SetList(this, cacheKey, rows);
        return rows;
    }

    /// <summary>Returns the SQL generated by the current expression query.</summary>
    public string ToSql() => BuildSql();

    /// <summary>Streams rows through the common DbDataReader + MSIL materialization pipeline.</summary>
    public async IAsyncEnumerable<T> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_includes.Count > 0)
        {
            var rows = await ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var row in rows) yield return row;
            yield break;
        }

        if (_db is ForgeDb forgeDb)
        {
            await foreach (var row in forgeDb.QueryStreamAsync<T>(BuildSql(), BuildParameters(), ExecutionOptions.TimeoutSeconds, cancellationToken).ConfigureAwait(false))
                yield return row;
            yield break;
        }

        var fallbackRows = await _db.QueryAsync<T>(BuildSql(), BuildParameters(), ExecutionOptions.TimeoutSeconds, cancellationToken).ConfigureAwait(false);
        foreach (var row in fallbackRows)
            yield return row;
    }

    /// <summary>Processes the current query in batches. This keeps caller code clean for million-record jobs.</summary>
    public async ValueTask ProcessInBatchesAsync(int batchSize, Func<IReadOnlyList<T>, ValueTask> processor, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (processor is null) throw new ArgumentNullException(nameof(processor));

        var batch = new List<T>(batchSize);
        await foreach (var row in StreamAsync(cancellationToken).ConfigureAwait(false))
        {
            batch.Add(row);
            if (batch.Count < batchSize) continue;
            await processor(batch.ToArray()).ConfigureAwait(false);
            batch.Clear();
        }

        if (batch.Count > 0)
            await processor(batch.ToArray()).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the FirstOrDefault operation.
    /// </summary>
    /// <returns>The result of the FirstOrDefault operation.</returns>
    public T? FirstOrDefault()
    {
        var row = _db.QueryFirstOrDefault<T>(BuildFirstSql(), BuildParameters(), ExecutionOptions.TimeoutSeconds);
        if (_includes.Count > 0 && row is not null)
            ForgeEfSplitGraphLoader.LoadIncludedNavigationsAsync(new[] { row }, _db, _includes, CancellationToken.None).GetAwaiter().GetResult();
        return row;
    }

    /// <summary>
    /// Executes the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FirstOrDefaultAsync operation.</returns>
    public async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.QueryFirstOrDefaultAsync<T>(BuildFirstSql(), BuildParameters(), ExecutionOptions.TimeoutSeconds, cancellationToken);
        if (_includes.Count > 0 && row is not null)
            await ForgeEfSplitGraphLoader.LoadIncludedNavigationsAsync(new[] { row }, _db, _includes, cancellationToken);
        return row;
    }

    /// <summary>
    /// Executes the Count operation.
    /// </summary>
    /// <returns>The result of the Count operation.</returns>
    public int Count() => _db.ExecuteScalar<int>(BuildCountSql(), BuildParameters(), ExecutionOptions.TimeoutSeconds);

    /// <summary>
    /// Executes the CountAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CountAsync operation.</returns>
    public async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => await _db.ExecuteScalarAsync<int>(BuildCountSql(), BuildParameters(), cancellationToken: cancellationToken);

    /// <summary>Executes SUM for the selected decimal column.</summary>
    public ValueTask<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("SUM", selector, cancellationToken);

    /// <summary>Executes AVG for the selected decimal column.</summary>
    public ValueTask<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("AVG", selector, cancellationToken);

    /// <summary>Executes MIN for the selected decimal column.</summary>
    public ValueTask<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("MIN", selector, cancellationToken);

    /// <summary>Executes MAX for the selected decimal column.</summary>
    public ValueTask<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("MAX", selector, cancellationToken);

    /// <summary>Executes expression-based paging using the current query filters and ordering.</summary>
    public async ValueTask<ForgePagedResult<T>> PageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        var totalRecords = await CountAsync(cancellationToken);
        var previousSkip = _skip;
        var previousTake = _take;

        try
        {
            _skip = (page - 1) * pageSize;
            _take = pageSize;

            var items = await ToListAsync(cancellationToken);
            return new ForgePagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }
        finally
        {
            _skip = previousSkip;
            _take = previousTake;
        }
    }

    private string BuildSql()
    {
        var key = BuildSqlCacheKey("LIST", _skip, _take, _orderBy, null);
        return QuerySqlCache.GetOrAdd(key, _ => BuildSqlCore());
    }

    private string BuildFirstSql()
    {
        var key = BuildSqlCacheKey("FIRST", 0, 1, _orderBy, null);
        return QuerySqlCache.GetOrAdd(key, _ => BuildFirstSqlCore());
    }

    private string BuildSqlCore()
    {
        var sql = BuildBaseSql();
        AppendOrderAndPaging(ref sql, _skip, _take, _orderBy);
        return sql;
    }

    private string BuildFirstSqlCore()
    {
        // Use TOP 1 instead of mutating Take(1) + OFFSET/FETCH. This removes list allocation and reduces SQL Server work.
        var sql = BuildBaseSql(top: 1);
        if (!string.IsNullOrWhiteSpace(_orderBy))
            sql += " ORDER BY " + _orderBy;
        return sql;
    }

    private static void AppendOrderAndPaging(ref string sql, int? skipValue, int? takeValue, string? orderBy)
    {
        var hasPaging = skipValue.HasValue || takeValue.HasValue;

        if (!string.IsNullOrWhiteSpace(orderBy))
            sql += " ORDER BY " + orderBy;
        else if (hasPaging)
            sql += " ORDER BY 1";

        if (!hasPaging)
            return;

        var skip = Math.Max(skipValue ?? 0, 0);
        var take = takeValue ?? 50;

        if (take <= 0)
            take = 1;

        if (skip == take)
            take += 1;

        sql += $" OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
    }

    private string BuildBaseSql(int? top = null)
    {
        var tag = string.IsNullOrWhiteSpace(ExecutionOptions.QueryTag) ? string.Empty : $"/* {ExecutionOptions.QueryTag.Replace("*/", string.Empty)} */ ";
        var tableExpression = _meta.TableName + BuildLockHint();
        var sql = _baseSql ?? $"{tag}SELECT {(top.HasValue ? "TOP " + top.Value + " " : string.Empty)}{BuildColumnList()} FROM {tableExpression}{(string.IsNullOrWhiteSpace(_temporalClause) ? string.Empty : " " + _temporalClause)}";
        if (_where.Count > 0) sql += " WHERE " + string.Join(" AND ", _where);
        return sql;
    }

    private string BuildLockHint()
    {
        return ExecutionOptions.LockBehavior switch
        {
            ForgeLockBehavior.NoLock => " WITH (NOLOCK)",
            ForgeLockBehavior.ReadPast => " WITH (READPAST)",
            ForgeLockBehavior.UpdateLock => " WITH (UPDLOCK)",
            ForgeLockBehavior.RowLock => " WITH (ROWLOCK)",
            ForgeLockBehavior.HoldLock => " WITH (HOLDLOCK)",
            _ => ExecutionOptions.ReadConsistency == ForgeORM.Abstractions.ForgeReadConsistency.ReadUncommitted ? " WITH (NOLOCK)" : string.Empty
        };
    }

    private string BuildColumnList()
    {
        var columns = _meta.Properties
            .Where(p => !p.IsComputed)
            .Select(p => p.ColumnName)
            .ToArray();

        return columns.Length == 0 ? "*" : string.Join(", ", columns);
    }

    private string BuildCountSql()
    {
        var key = BuildSqlCacheKey("COUNT", null, null, null, null);
        return CountSqlCache.GetOrAdd(key, _ => "SELECT COUNT(1) FROM (" + BuildBaseSql() + ") ForgeCount");
    }

    private string BuildAnySql()
    {
        var key = BuildSqlCacheKey("ANY", null, null, null, null);
        return AnySqlCache.GetOrAdd(key, _ => "SELECT CASE WHEN EXISTS (" + BuildBaseSql() + ") THEN 1 ELSE 0 END");
    }

    private string BuildAggregateSql(string function, LambdaExpression selector)
    {
        var column = ForgeExpressionTranslator.MemberName(selector);
        var key = BuildSqlCacheKey("AGG", null, null, null, function + ":" + column);
        return AggregateSqlCache.GetOrAdd(key, _ => $"SELECT COALESCE({function}({column}), 0) FROM (" + BuildBaseSql() + ") ForgeAggregate");
    }

    private string BuildSqlCacheKey(string operation, int? skip, int? take, string? orderBy, string? extra)
    {
        return string.Join("|",
            typeof(T).FullName,
            operation,
            _baseSql ?? string.Empty,
            _meta.TableName,
            string.Join("&", _where),
            _temporalClause ?? string.Empty,
            orderBy ?? string.Empty,
            skip?.ToString() ?? string.Empty,
            take?.ToString() ?? string.Empty,
            extra ?? string.Empty);
    }

    private async ValueTask<decimal> ExecuteDecimalAggregateAsync(
        string function,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken)
    {
        return await _db.ExecuteScalarAsync<decimal>(
            BuildAggregateSql(function, selector),
            BuildParameters(),
            ExecutionOptions.TimeoutSeconds,
            cancellationToken);
    }

    private object? BuildParameters()
    {
        if (_temporalParameters.Count == 0)
            return _parameters.Count == 0 ? _baseParameters : _parameters;

        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (_baseParameters is IReadOnlyDictionary<string, object?> readonlyDictionary)
        {
            foreach (var item in readonlyDictionary) merged[item.Key] = item.Value;
        }
        else if (_baseParameters is IDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary) merged[item.Key] = item.Value;
        }
        else if (_baseParameters is not null)
        {
            foreach (var property in _baseParameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead))
                merged[property.Name] = ForgeRuntimeAccessorCache.Get(property, _baseParameters);
        }

        foreach (var item in _parameters) merged[item.Key] = item.Value;
        foreach (var item in _temporalParameters) merged[item.Key] = item.Value;
        return merged;
    }

    private void MergeParameters(object? parameters)
    {
        if (parameters is null) return;
        if (parameters is IReadOnlyDictionary<string, object?> readonlyDictionary)
        {
            foreach (var item in readonlyDictionary) _parameters[item.Key] = item.Value;
            return;
        }
        if (parameters is IDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary) _parameters[item.Key] = item.Value;
            return;
        }
        foreach (var property in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead))
            _parameters[property.Name] = ForgeRuntimeAccessorCache.Get(property, parameters);
    }
}
