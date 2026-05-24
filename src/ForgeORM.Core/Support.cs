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

internal sealed class ForgeGridReader : IForgeGridReader
{
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbDataReader _reader;
    private bool _hasConsumedCurrentResult;

    /// <summary>
    /// Executes the ForgeGridReader operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="command">The command value.</param>
    /// <param name="reader">The reader value.</param>
    /// <returns>The result of the ForgeGridReader operation.</returns>
    public ForgeGridReader(DbConnection connection, DbCommand command, DbDataReader reader)
    {
        _connection = connection;
        _command = command;
        _reader = reader;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IEnumerable<T> Read<T>() => ReadAsync<T>().GetAwaiter().GetResult();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask<IReadOnlyList<T>> ReadAsync<T>()
    {
        if (_hasConsumedCurrentResult)
            await _reader.NextResultAsync();

        var rows = new List<T>();
        var materializer = ForgeIlMaterializerCache.GetOrCreate<T>(_reader);
        while (await _reader.ReadAsync())
            rows.Add(materializer(_reader));

        _hasConsumedCurrentResult = true;
        return rows;
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
        _reader.Dispose();
        _command.Dispose();
        _connection.Dispose();
    }
}

public sealed class ReflectionForgeEntityMetadataResolver : IForgeEntityMetadataResolver
{
    private readonly ConcurrentDictionary<Type, ForgeEntityMetadata> _cache = new();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));
    /// <summary>
    /// Executes the Resolve operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the Resolve operation.</returns>
    public ForgeEntityMetadata Resolve(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Framework-level policy: generated metadata always wins when available, even when
        // older app code still constructs ReflectionForgeEntityMetadataResolver directly.
        // Reflection becomes the safe fallback, not a separate runtime framework.
        if (ForgeSourceGeneratedRegistry.TryGetMetadata(type, out var generated))
            return generated;

        return _cache.GetOrAdd(type, BuildMetadata);
    }

    private static ForgeEntityMetadata BuildMetadata(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && ForgeMaterializer.IsScalar(p.PropertyType))
            .ToArray();

        var keyProperty = ResolveKeyProperty(type, properties);

        var props = properties
            .Select(p => new ForgePropertyMetadata
            {
                PropertyName = p.Name,
                ColumnName = p.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? p.Name,
                PropertyType = p.PropertyType,
                IsKey = ReferenceEquals(p, keyProperty),
                IsCode = p.GetCustomAttribute<ForgeCodeAttribute>() is not null || p.Name.Equals("Code", StringComparison.OrdinalIgnoreCase),
                IsComputed = p.GetCustomAttribute<ForgeComputedAttribute>() is not null
            }).ToList();

        return new ForgeEntityMetadata
        {
            EntityType = type,
            TableName = type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name,
            KeyColumn = props.FirstOrDefault(x => x.IsKey)?.ColumnName ?? "Id",
            CodeColumn = props.FirstOrDefault(x => x.IsCode)?.ColumnName ?? "Code",
            Properties = props
        };
    }

    private static PropertyInfo? ResolveKeyProperty(Type type, PropertyInfo[] properties)
    {
        // Attribute-first when users opt in. Dapper-like convention still works without attributes.
        var explicitKey = properties.FirstOrDefault(p =>
            p.GetCustomAttribute<ForgeKeyAttribute>() is not null ||
            p.GetCustomAttribute<KeyAttribute>() is not null);
        if (explicitKey is not null)
            return explicitKey;

        var id = properties.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (id is not null)
            return id;

        var entityId = properties.FirstOrDefault(p => p.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase));
        if (entityId is not null)
            return entityId;

        // Common record/DTO convention: OrderSummaryRecord(OrderId, ...).
        var suffixId = properties.FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
        return suffixId;
    }
}

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

internal static class ForgeNavigationSupport
{
    public static async ValueTask LoadIncludedNavigationsAsync<T>(
        IReadOnlyList<T> rows,
        IForgeDb db,
        string keyColumn,
        IReadOnlyList<PropertyInfo> includes,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0 || includes.Count == 0)
            return;

        foreach (var navigation in includes)
        {
            if (!navigation.CanWrite)
                continue;

            if (IsCollectionNavigation(navigation))
            {
                await LoadCollectionNavigationAsync(rows, db, keyColumn, navigation, cancellationToken);
                continue;
            }

            if (IsReferenceNavigation(navigation))
                await LoadReferenceNavigationAsync(rows, db, navigation, cancellationToken);
        }
    }

    public static bool IsCollectionNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
            return false;

        return property.PropertyType.IsGenericType
            && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
    }

    public static bool IsReferenceNavigation(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
            return false;

        if (IsCollectionNavigation(property))
            return false;

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type.IsClass && !IsScalarColumnType(type);
    }

    private static async ValueTask LoadCollectionNavigationAsync<T>(
        IReadOnlyList<T> parents,
        IForgeDb db,
        string keyColumn,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        if (parents.Count == 0)
            return;

        var childType = navigation.PropertyType.GetGenericArguments()[0];
        var parentKey = typeof(T).GetProperty(keyColumn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (parentKey is null)
            return;

        var childForeignKeyName = typeof(T).Name + "Id";
        var childForeignKey = childType.GetProperty(childForeignKeyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (childForeignKey is null)
            return;

        var ids = parents
            .Select(parent => ForgeRuntimeAccessorCache.Get(parentKey, parent!))
            .Where(x => x is not null)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return;

        var childTable = ResolveTableName(childType);
        var childColumns = ResolveScalarColumns(childType);
        var sql = $"SELECT {childColumns} FROM {childTable} WHERE {childForeignKey.Name} IN @Ids";

        var queryAsync = typeof(IForgeDb).GetMethods()
            .Where(x => x.Name == nameof(IForgeDb.QueryAsync) && x.IsGenericMethodDefinition)
            .First(x => x.GetParameters().Length >= 2)
            .MakeGenericMethod(childType);

        var awaitable = queryAsync.Invoke(db, new object?[] { sql, new { Ids = ids }, null, cancellationToken })!;
        var result = await ForgeRuntimeMemberCache.AwaitAndGetResultAsync(awaitable).ConfigureAwait(false)
            as System.Collections.IEnumerable;
        if (result is null)
            return;

        var children = result.Cast<object>().ToList();
        foreach (var parent in parents)
        {
            var parentId = ForgeRuntimeAccessorCache.Get(parentKey, parent!);
            var list = (System.Collections.IList)ForgeRuntimeAccessorCache.Constructor(typeof(List<>).MakeGenericType(childType))();

            foreach (var child in children)
            {
                var fk = ForgeRuntimeAccessorCache.Get(childForeignKey, child);
                if (Equals(fk, parentId))
                    list.Add(child);
            }

            ForgeRuntimeAccessorCache.Set(navigation, parent!, list);
        }
    }

    private static async ValueTask LoadReferenceNavigationAsync<T>(
        IReadOnlyList<T> parents,
        IForgeDb db,
        PropertyInfo navigation,
        CancellationToken cancellationToken)
    {
        var childType = navigation.PropertyType;
        var fkProperty = typeof(T).GetProperty(navigation.Name + "Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (fkProperty is null)
            return;

        var childTable = ResolveTableName(childType);
        var childColumns = ResolveScalarColumns(childType);

        var queryFirstOrDefaultAsync = typeof(IForgeDb).GetMethods()
            .Where(x => x.Name == nameof(IForgeDb.QueryFirstOrDefaultAsync) && x.IsGenericMethodDefinition)
            .First(x => x.GetParameters().Length >= 2)
            .MakeGenericMethod(childType);

        foreach (var parent in parents)
        {
            var fkValue = ForgeRuntimeAccessorCache.Get(fkProperty, parent!);
            if (fkValue is null)
                continue;

            var sql = $"SELECT {childColumns} FROM {childTable} WHERE Id = @Id";
            var awaitable = queryFirstOrDefaultAsync.Invoke(db, new object?[] { sql, new { Id = fkValue }, null, cancellationToken })!;
            var child = await ForgeRuntimeMemberCache.AwaitAndGetResultAsync(awaitable).ConfigureAwait(false);
            ForgeRuntimeAccessorCache.Set(navigation, parent!, child);
        }
    }

    private static string ResolveTableName(Type type)
        => type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name;

    private static string ResolveScalarColumns(Type type)
    {
        var columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => IsScalarColumnType(x.PropertyType))
            .Select(x => x.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return columns.Length == 0 ? "*" : string.Join(", ", columns);
    }

    private static bool IsScalarColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(byte[]);
    }
}

internal static class ForgeExpressionTranslator
{
    private static readonly ConcurrentDictionary<string, string> PredicateSqlCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string> MemberNameCache = new(StringComparer.Ordinal);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        var cacheKey = typeof(T).FullName + ":" + expression;
        return PredicateSqlCache.GetOrAdd(cacheKey, _ => TranslateCore(expression.Body));
    }

    private static string TranslateCore(Expression body)
    {
        if (body is not BinaryExpression b) throw new NotSupportedException("Only simple binary expressions are supported in MVP.");
        return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        var cacheKey = typeof(T).FullName + ":" + expression;
        return MemberNameCache.GetOrAdd(cacheKey, _ => MemberNameCore(expression));
    }

    public static string MemberName(LambdaExpression expression)
    {
        var cacheKey = expression.ReturnType.FullName + ":" + expression;
        return MemberNameCache.GetOrAdd(cacheKey, _ => MemberNameCore(expression));
    }

    private static string MemberNameCore(LambdaExpression expression)
    {
        Expression body = expression.Body is UnaryExpression u ? u.Operand : expression.Body;
        return body is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Only member expression is supported.");
    }
    private static string Member(Expression e) => e is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Left side must be member.");
    private static string Operator(ExpressionType t) => t switch { ExpressionType.Equal => "=", ExpressionType.NotEqual => "<>", ExpressionType.GreaterThan => ">", ExpressionType.GreaterThanOrEqual => ">=", ExpressionType.LessThan => "<", ExpressionType.LessThanOrEqual => "<=", _ => throw new NotSupportedException("Operator not supported.") };
    private static string Value(Expression e)
    {
        var v = ForgeExpressionDelegateCache.Evaluate(e);
        return v switch { null => "NULL", string s => "'" + s.Replace("'", "''") + "'", DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss") + "'", bool b => b ? "1" : "0", _ => v?.ToString() ?? "NULL" };
    }
}

internal sealed class ForgeSplitQuery<TParent> : IForgeSplitQuery<TParent>
{
    private readonly IForgeDb _db;
    private readonly List<Func<IReadOnlyList<TParent>, CancellationToken, ValueTask>> _includes = [];

    /// <summary>
    /// Executes the ForgeSplitQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the ForgeSplitQuery operation.</returns>
    public ForgeSplitQuery(IForgeDb db) => _db = db;

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="childSqlFactory">The childSqlFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="assign">The assign value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public IForgeSplitQuery<TParent> IncludeMany<TChild, TKey>(
        Func<IReadOnlyCollection<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        where TKey : notnull
    {
        _includes.Add(async (parents, ct) =>
        {
            var ids = parents.Select(parentKey).Distinct().ToList();
            if (ids.Count == 0) return;

            var children = await _db.QueryAsync<TChild>(childSqlFactory(ids), new { Ids = ids }, cancellationToken: ct);
            var lookup = children.GroupBy(childForeignKey).ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
                assign(parent, lookup.TryGetValue(parentKey(parent), out var rows) ? rows : Array.Empty<TChild>());
        });

        return this;
    }

    /// <summary>
    /// Executes the TChild operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <param name="childTable">The childTable value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="target">The target value.</param>
    /// <param name="backingField">The backingField value.</param>
    /// <param name="childWhereSql">The childWhereSql value.</param>
    /// <returns>The result of the TChild operation.</returns>
    public IForgeSplitQuery<TParent> IncludeMany<TChild>(
        string childTable,
        string parentKey = "Id",
        string childForeignKey = "ParentId",
        Expression<Func<TParent, IEnumerable<TChild>>>? target = null,
        string? backingField = null,
        string? childWhereSql = null)
    {
        var parentKeyProperty = FindProperty(typeof(TParent), parentKey);
        var childForeignKeyProperty = FindProperty(typeof(TChild), childForeignKey);

        _includes.Add(async (parents, ct) =>
        {
            var ids = parents
                .Select(x => ForgeRuntimeAccessorCache.Get(parentKeyProperty, x!))
                .Where(x => x is not null)
                .Distinct()
                .ToList();

            if (ids.Count == 0) return;

            var sql = $"SELECT * FROM {childTable} WHERE {childForeignKey} IN @Ids";
            if (!string.IsNullOrWhiteSpace(childWhereSql))
                sql += " AND " + childWhereSql;

            var children = await _db.QueryAsync<TChild>(sql, new { Ids = ids }, cancellationToken: ct);
            var lookup = children
                .GroupBy(x => ForgeRuntimeAccessorCache.Get(childForeignKeyProperty, x!))
                .ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
            {
                var key = ForgeRuntimeAccessorCache.Get(parentKeyProperty, parent!);
                var rows = key is not null && lookup.TryGetValue(key, out var found) ? found : Array.Empty<TChild>();
                AssignChildren(parent, rows, target, backingField);
            }
        });

        return this;
    }

    /// <summary>
    /// Executes the Any operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Any operation.</returns>
    public bool Any(string parentSql, object? parameters = null)
        => _db.ExecuteScalar<int>($"SELECT CASE WHEN EXISTS ({parentSql}) THEN 1 ELSE 0 END", parameters) > 0;

    /// <summary>
    /// Executes the AnyAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AnyAsync operation.</returns>
    public async ValueTask<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
        => await _db.ExecuteScalarAsync<int>($"SELECT CASE WHEN EXISTS ({parentSql}) THEN 1 ELSE 0 END", parameters, cancellationToken: cancellationToken) > 0;

    /// <summary>
    /// Executes the FirstOrDefault operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the FirstOrDefault operation.</returns>
    public TParent? FirstOrDefault(string parentSql, object? parameters = null)
        => FirstOrDefaultAsync(parentSql, parameters).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FirstOrDefaultAsync operation.</returns>
    public async ValueTask<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
        => (await ToListAsync(parentSql, parameters, cancellationToken)).FirstOrDefault();

    /// <summary>
    /// Executes the ToList operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the ToList operation.</returns>
    public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null)
        => ToListAsync(parentSql, parameters).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public async ValueTask<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var parents = (await _db.QueryAsync<TParent>(parentSql, parameters, cancellationToken: cancellationToken)).ToList();
        foreach (var include in _includes)
            await include(parents, cancellationToken);
        return parents;
    }

    private static PropertyInfo FindProperty(Type type, string name)
        => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
           ?? throw new InvalidOperationException($"Property '{name}' was not found on {type.Name}.");

    private static void AssignChildren<TChild>(
        TParent parent,
        IReadOnlyList<TChild> children,
        Expression<Func<TParent, IEnumerable<TChild>>>? target,
        string? backingField)
    {
        if (!string.IsNullOrWhiteSpace(backingField))
        {
            var field = typeof(TParent).GetField(backingField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException($"Backing field '{backingField}' was not found on {typeof(TParent).Name}.");

            if (ForgeRuntimeMemberCache.Get(field, parent!) is IList<TChild> list)
            {
                list.Clear();
                foreach (var child in children) list.Add(child);
                return;
            }

            ForgeRuntimeMemberCache.Set(field, parent!, children.ToList());
            return;
        }

        if (target is null)
            return;

        var member = target.Body is MemberExpression m ? m : null;
        if (member?.Member is not PropertyInfo property)
            throw new InvalidOperationException("Target must be a collection property expression, for example x => x.Items.");

        if (property.CanWrite)
        {
            ForgeRuntimeAccessorCache.Set(property, parent!, ConvertChildren(children, property.PropertyType));
            return;
        }

        if (ForgeRuntimeAccessorCache.Get(property, parent!) is IList<TChild> existing)
        {
            existing.Clear();
            foreach (var child in children) existing.Add(child);
        }
    }

    private static object ConvertChildren<TChild>(IReadOnlyList<TChild> children, Type targetType)
    {
        if (targetType.IsAssignableFrom(children.GetType())) return children;
        if (targetType.IsAssignableFrom(typeof(List<TChild>))) return children.ToList();
        if (targetType.IsArray) return children.ToArray();
        return children.ToList();
    }
}

public sealed partial class ForgeTransaction : IForgeTransaction
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    private ForgeTransaction(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection; _transaction = transaction;
    }

    /// <summary>
    /// Executes the Begin operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <returns>The result of the Begin operation.</returns>
    public static ForgeTransaction Begin(DbConnection connection) => new(connection, connection.BeginTransaction());
    /// <summary>
    /// Executes the BeginAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The result of the BeginAsync operation.</returns>
    public static async ValueTask<ForgeTransaction> BeginAsync(DbConnection connection, CancellationToken ct) => new(connection, await connection.BeginTransactionAsync(ct));

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Query<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds).ToList();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.QueryAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Execute(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public ValueTask<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeAdo.ExecuteAsync(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.ExecuteScalar<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.ExecuteScalarAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    /// <summary>
    /// Executes the Commit operation.
    /// </summary>
    public void Commit() => _transaction.Commit();
    /// <summary>
    /// Executes the CommitAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CommitAsync operation.</returns>
    public ValueTask CommitAsync(CancellationToken cancellationToken = default)
    => new(_transaction.CommitAsync(cancellationToken));
    /// <summary>
    /// Executes the Rollback operation.
    /// </summary>
    public void Rollback() => _transaction.Rollback();
    /// <summary>
    /// Executes the RollbackAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RollbackAsync operation.</returns>
    public ValueTask RollbackAsync(CancellationToken cancellationToken = default) => new(_transaction.RollbackAsync(cancellationToken));
    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    public void Dispose() { _transaction.Dispose(); _connection.Dispose(); }
    /// <summary>
    /// Executes the DisposeAsync operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    /// <returns>The result of the DisposeAsync operation.</returns>
    public async ValueTask DisposeAsync() { await _transaction.DisposeAsync(); await _connection.DisposeAsync(); }
}
