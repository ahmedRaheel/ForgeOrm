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

    public ForgeSmartQuery(IForgeDb db, string sql, object? parameters = null, IMemoryCache? cache = null)
    {
        _db = db;
        _sql = sql;
        _baseParameters = parameters;
        _cache = cache;
    }

    public IForgeSmartQuery<T> WhereSql(FormattableString sql)
    {
        var safe = ForgeSqlSafety.From(sql);
        _whereSql.Add(safe.Sql);
        foreach (var item in safe.Parameters)
            _parameters[item.Key] = item.Value;
        return this;
    }

    public IForgeSmartQuery<T> WithPolicy(ForgeResiliencePolicy policy)
    {
        _policy = policy;
        return this;
    }

    public IForgeSmartQuery<T> AsCached(TimeSpan duration, string? key = null)
    {
        _cacheOptions = new ForgeCacheOptions { Duration = duration, Key = key };
        return this;
    }

    public IForgeSmartQuery<T> Mock(IEnumerable<T> rows)
    {
        _mockRows = rows.ToList();
        return this;
    }

    public IForgeSmartQuery<T> IncludeGraph(int maxDepth = 2)
    {
        _includeGraphDepth = maxDepth;
        return this;
    }

    public IForgeSmartQuery<T> ShadowProperty(string name)
    {
        _shadowProperties.Add(name);
        return this;
    }

    public ForgeTransparentCommand ExecuteTransparent()
    {
        return new ForgeTransparentCommand
        {
            Sql = BuildSql(),
            Parameters = BuildParameters()
        };
    }

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

    public Task<ForgeExplainResult> ExplainAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Explain());
    }

    public IReadOnlyList<TShape> ToShape<TShape>()
    {
        return ExecuteWithPolicy(() => _db.Query<TShape>(BuildSql(), BuildParameters()).ToList());
    }

    public Task<IReadOnlyList<TShape>> ToShapeAsync<TShape>(CancellationToken cancellationToken = default)
    {
        return ExecuteWithPolicyAsync(() => _db.QueryAsync<TShape>(BuildSql(), BuildParameters(), cancellationToken: cancellationToken));
    }

    public IReadOnlyList<TShape> MapStatic<TShape>() => ToShape<TShape>();
    public Task<IReadOnlyList<TShape>> MapStaticAsync<TShape>(CancellationToken cancellationToken = default) => ToShapeAsync<TShape>(cancellationToken);

    public JsonDocument IntoJsonDocument()
    {
        return JsonDocument.Parse(IntoJson());
    }

    public async Task<JsonDocument> IntoJsonDocumentAsync(CancellationToken cancellationToken = default)
    {
        return JsonDocument.Parse(await IntoJsonAsync(cancellationToken));
    }

    public string IntoJson()
    {
        var rows = ToList();
        return JsonSerializer.Serialize(rows);
    }

    public async Task<string> IntoJsonAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ToListAsync(cancellationToken);
        return JsonSerializer.Serialize(rows);
    }

    public async IAsyncEnumerable<T> StreamAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rows = await ToListAsync(cancellationToken);
        foreach (var row in rows)
            yield return row;
    }

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
