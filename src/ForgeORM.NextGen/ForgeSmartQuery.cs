using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeSmartQuery<T> : IForgeSmartQuery<T>
{
    private readonly IForgeDb _db;
    private readonly IMemoryCache? _cache;
    private readonly List<string> _whereSql = [];
    private readonly Dictionary<string, object?> _parameters = [];
    private readonly List<string> _shadowProperties = [];
    private string _sql;
    private object? _baseParameters;
    private ForgeCacheOptions? _cacheOptions;
    private ForgeResiliencePolicy? _policy;
    private IReadOnlyList<T>? _mockRows;
    private int? _includeGraphDepth;

    /// <summary>
    /// Executes the ForgeSmartQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cache">The cache value.</param>
    /// <returns>The result of the ForgeSmartQuery operation.</returns>
    public ForgeSmartQuery(IForgeDb db, string sql, object? parameters = null, IMemoryCache? cache = null)
    {
        _db = db;
        _sql = sql;
        _baseParameters = parameters;
        _cache = cache;
    }

    /// <summary>
    /// Executes the WhereSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    public IForgeSmartQuery<T> WhereSql(FormattableString sql)
    {
        var safe = ForgeSqlSafety.From(sql);
        _whereSql.Add(safe.Sql);
        foreach (var item in safe.Parameters)
            _parameters[item.Key] = item.Value;
        return this;
    }

    /// <summary>
    /// Executes the WithPolicy operation.
    /// </summary>
    /// <param name="policy">The policy value.</param>
    /// <returns>The result of the WithPolicy operation.</returns>
    public IForgeSmartQuery<T> WithPolicy(ForgeResiliencePolicy policy)
    {
        _policy = policy;
        return this;
    }

    /// <summary>
    /// Executes the AsCached operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the AsCached operation.</returns>
    public IForgeSmartQuery<T> AsCached(TimeSpan duration, string? key = null)
    {
        _cacheOptions = new ForgeCacheOptions { Duration = duration, Key = key };
        return this;
    }

    /// <summary>
    /// Executes the Mock operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Mock operation.</returns>
    public IForgeSmartQuery<T> Mock(IEnumerable<T> rows)
    {
        _mockRows = rows.ToList();
        return this;
    }

    /// <summary>
    /// Executes the IncludeGraph operation.
    /// </summary>
    /// <param name="maxDepth">The maxDepth value.</param>
    /// <returns>The result of the IncludeGraph operation.</returns>
    public IForgeSmartQuery<T> IncludeGraph(int maxDepth = 2)
    {
        _includeGraphDepth = maxDepth;
        return this;
    }

    /// <summary>
    /// Executes the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    public IForgeSmartQuery<T> ShadowProperty(string name)
    {
        _shadowProperties.Add(name);
        return this;
    }

    /// <summary>
    /// Executes the ExecuteTransparent operation.
    /// </summary>
    /// <returns>The result of the ExecuteTransparent operation.</returns>
    public ForgeTransparentCommand ExecuteTransparent()
    {
        return new ForgeTransparentCommand
        {
            Sql = BuildSql(),
            Parameters = BuildParameters()
        };
    }

    /// <summary>
    /// Executes the Explain operation.
    /// </summary>
    /// <returns>The result of the Explain operation.</returns>
    public ForgeExplainResult Explain()
    {
        var sql = BuildSql();
        var analysis = _db.Analyze(sql);
        return new ForgeExplainResult
        {
            Sql = BuildExplainSql(sql),
            ProviderName = _db.Provider.ProviderName,
            Warnings = analysis.Warnings,
            Suggestions = analysis.Suggestions,
            RawPlan = "Provider-specific execution plan hook. Use ExplainAsync against a live provider to execute this SQL."
        };
    }

    /// <summary>
    /// Executes the ExplainAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExplainAsync operation.</returns>
    public Task<ForgeExplainResult> ExplainAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Explain());
    }

    /// <summary>
    /// Executes the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <returns>The result of the TShape operation.</returns>
    public IReadOnlyList<TShape> ToShape<TShape>()
    {
        return ExecuteWithPolicy(() => _db.Query<TShape>(BuildSql(), BuildParameters()).ToList());
    }

    /// <summary>
    /// Executes the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TShape operation.</returns>
    public Task<IReadOnlyList<TShape>> ToShapeAsync<TShape>(CancellationToken cancellationToken = default)
    {
        return ExecuteWithPolicyAsync(() => _db.QueryAsync<TShape>(BuildSql(), BuildParameters(), cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Executes the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <returns>The result of the TShape operation.</returns>
    public IReadOnlyList<TShape> MapStatic<TShape>() => ToShape<TShape>();
    /// <summary>
    /// Executes the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TShape operation.</returns>
    public Task<IReadOnlyList<TShape>> MapStaticAsync<TShape>(CancellationToken cancellationToken = default) => ToShapeAsync<TShape>(cancellationToken);

    /// <summary>
    /// Executes the IntoJsonDocument operation.
    /// </summary>
    /// <returns>The result of the IntoJsonDocument operation.</returns>
    public JsonDocument IntoJsonDocument()
    {
        return JsonDocument.Parse(IntoJson());
    }

    /// <summary>
    /// Executes the IntoJsonDocumentAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IntoJsonDocumentAsync operation.</returns>
    public async Task<JsonDocument> IntoJsonDocumentAsync(CancellationToken cancellationToken = default)
    {
        return JsonDocument.Parse(await IntoJsonAsync(cancellationToken));
    }

    /// <summary>
    /// Executes the IntoJson operation.
    /// </summary>
    /// <returns>The result of the IntoJson operation.</returns>
    public string IntoJson()
    {
        var rows = ToList();
        return JsonSerializer.Serialize(rows);
    }

    /// <summary>
    /// Executes the IntoJsonAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IntoJsonAsync operation.</returns>
    public async Task<string> IntoJsonAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ToListAsync(cancellationToken);
        return JsonSerializer.Serialize(rows);
    }

    /// <summary>
    /// Executes the StreamAllAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the StreamAllAsync operation.</returns>
    public async IAsyncEnumerable<T> StreamAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rows = await ToListAsync(cancellationToken);
        foreach (var row in rows)
            yield return row;
    }

    /// <summary>
    /// Executes the ToList operation.
    /// </summary>
    /// <returns>The result of the ToList operation.</returns>
    public IReadOnlyList<T> ToList()
    {
        if (_mockRows is not null)
            return _mockRows;

        var key = BuildCacheKey();
        if (_cache is not null && _cacheOptions is not null && _cache.TryGetValue(key, out IReadOnlyList<T>? cached) && cached is not null)
            return cached;

        var result = ExecuteWithPolicy(() => _db.Query<T>(BuildSql(), BuildParameters()).ToList());

        if (_cache is not null && _cacheOptions is not null)
            _cache.Set(key, result, _cacheOptions.Duration);

        return result;
    }

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public async Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        if (_mockRows is not null)
            return _mockRows;

        var key = BuildCacheKey();
        if (_cache is not null && _cacheOptions is not null && _cache.TryGetValue(key, out IReadOnlyList<T>? cached) && cached is not null)
            return cached;

        var result = await ExecuteWithPolicyAsync(() => _db.QueryAsync<T>(BuildSql(), BuildParameters(), cancellationToken: cancellationToken));

        if (_cache is not null && _cacheOptions is not null)
            _cache.Set(key, result, _cacheOptions.Duration);

        return result;
    }

    private string BuildSql()
    {
        if (_whereSql.Count == 0)
            return _sql;

        var glue = _sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase) ? " AND " : " WHERE ";
        return _sql + glue + string.Join(" AND ", _whereSql.Select(x => "(" + x + ")"));
    }

    private object? BuildParameters()
    {
        if (_parameters.Count == 0)
            return _baseParameters;

        return _parameters;
    }

    private string BuildCacheKey()
    {
        if (!string.IsNullOrWhiteSpace(_cacheOptions?.Key))
            return _cacheOptions.Key!;

        return typeof(T).FullName + ":" + BuildSql() + ":" + JsonSerializer.Serialize(BuildParameters());
    }

    private string BuildExplainSql(string sql)
    {
        return _db.Provider.ProviderName switch
        {
            "SqlServer" => "SET SHOWPLAN_XML ON; " + sql,
            "PostgreSql" => "EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + sql,
            "MySql" => "EXPLAIN " + sql,
            "Oracle" => "EXPLAIN PLAN FOR " + sql,
            "Sqlite" => "EXPLAIN QUERY PLAN " + sql,
            _ => "EXPLAIN " + sql
        };
    }

    private TResult ExecuteWithPolicy<TResult>(Func<TResult> action)
    {
        var retry = _policy?.RetryCount ?? 0;
        var delay = _policy?.RetryDelay ?? TimeSpan.Zero;

        for (var attempt = 0; ; attempt++)
        {
            try { return action(); }
            catch when (attempt < retry)
            {
                if (delay > TimeSpan.Zero) Thread.Sleep(delay);
            }
        }
    }

    private async Task<TResult> ExecuteWithPolicyAsync<TResult>(Func<Task<TResult>> action)
    {
        var retry = _policy?.RetryCount ?? 0;
        var delay = _policy?.RetryDelay ?? TimeSpan.Zero;

        for (var attempt = 0; ; attempt++)
        {
            try { return await action(); }
            catch when (attempt < retry)
            {
                if (delay > TimeSpan.Zero) await Task.Delay(delay);
            }
        }
    }
}
