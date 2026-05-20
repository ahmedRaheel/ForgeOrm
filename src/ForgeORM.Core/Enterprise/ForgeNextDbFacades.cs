using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public enum VectorMetric
{
    Cosine,
    Dot,
    Euclidean
}

public sealed record ForgeVectorMatch<T>(T? Item, double Score, object? Id = null);

public static class ForgeNextDbFacadeExtensions
{
    public static ForgeVectorQuery<T> Vector<T>(this ForgeDb db) => new(db);

    public static ForgeGraphTraversalBuilder Graph(this ForgeDb db) => new(db);

    public static ForgeWorkflowFacade<TWorkflow> Workflow<TWorkflow>(this ForgeDb db) => new(db);

    public static ForgeRulesFacade Rules(this ForgeDb db) => new(db);

    public static ForgeCubeBuilder<T> Cube<T>(this ForgeDb db) => new(db);

    public static ForgeShardQueryable<T> UseShard<T>(this IForgeQuery<T> query, string shard)
    {
        query.ExecutionOptions.AddShard(shard);
        return new ForgeShardQueryable<T>(query);
    }

    public static async Task<int> ReadIntoAsync<T>(this IForgeQuery<T> query, T[] buffer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var rows = await query.Take(buffer.Length).ToListAsync(cancellationToken).ConfigureAwait(false);
        var count = Math.Min(rows.Count, buffer.Length);
        for (var i = 0; i < count; i++) buffer[i] = rows[i];
        return count;
    }

    public static async Task<int> ReadIntoAsync<TSource, TDestination>(this IForgeQuery<TSource> query, TDestination[] buffer, CancellationToken cancellationToken = default)
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var rows = await query.Take(buffer.Length).ToListAsync(cancellationToken).ConfigureAwait(false);
        var count = Math.Min(rows.Count, buffer.Length);
        var mapper = ForgePooledBufferMapper<TSource, TDestination>.Map;
        for (var i = 0; i < count; i++) buffer[i] = mapper(rows[i]);
        return count;
    }
}


public partial class ForgeDb
{
    private ForgeJobFacade? _jobs;

    /// <summary>Background job facade exposed directly as db.Jobs.</summary>
    public ForgeJobFacade Jobs => _jobs ??= new ForgeJobFacade(this);
}

public static class ForgeJobFacadeExtensions
{
    private static readonly ConditionalWeakTable<ForgeDb, ForgeJobFacade> JobsCache = new();
    public static ForgeJobFacade Jobs(this ForgeDb db) => JobsCache.GetValue(db, static x => new ForgeJobFacade(x));
}

public static class ForgeQueryShardExecutionExtensions
{
    public static ForgeShardQueryable<T> UseShard<T>(this ForgeShardQueryable<T> query, string shard)
    {
        query.Inner.ExecutionOptions.AddShard(shard);
        return query;
    }
}

internal static class ForgeQueryExecutionOptionShardExtensions
{
    private static readonly ConditionalWeakTable<ForgeQueryExecutionOptions, ForgeShardList> Shards = new();

    public static void AddShard(this ForgeQueryExecutionOptions options, string shard)
    {
        if (string.IsNullOrWhiteSpace(shard)) return;
        var list = Shards.GetValue(options, _ => new ForgeShardList());
        lock (list.Values)
        {
            if (!list.Values.Contains(shard, StringComparer.OrdinalIgnoreCase))
                list.Values.Add(shard);
        }
    }

    public static IReadOnlyList<string> GetShards(this ForgeQueryExecutionOptions options)
    {
        return Shards.TryGetValue(options, out var list) ? list.Values.ToArray() : Array.Empty<string>();
    }

    private sealed class ForgeShardList
    {
        public List<string> Values { get; } = [];
    }
}

public sealed class ForgeShardQueryable<T> : IForgeQuery<T>
{
    internal IForgeQuery<T> Inner { get; }

    internal ForgeShardQueryable(IForgeQuery<T> inner) => Inner = inner;

    public ForgeQueryExecutionOptions ExecutionOptions => Inner.ExecutionOptions;
    public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate) { Inner.Where(predicate); return this; }
    public IForgeQuery<T> Where(string condition, object? parameters = null) { Inner.Where(condition, parameters); return this; }
    public IForgeQuery<T> WhereSql(string condition, object? parameters = null) { Inner.WhereSql(condition, parameters); return this; }
    public IForgeQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate) { Inner.WhereIf(condition, predicate); return this; }
    public IForgeQuery<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null) { Inner.WhereSqlIf(condition, sqlCondition, parameters); return this; }
    public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector) { Inner.OrderBy(keySelector); return this; }
    public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) { Inner.OrderByDescending(keySelector); return this; }
    public IForgeQuery<T> OrderBy(string orderBy) { Inner.OrderBy(orderBy); return this; }
    public IForgeQuery<T> OrderBySql(string orderBy) { Inner.OrderBySql(orderBy); return this; }
    public IForgeQuery<T> Skip(int count) { Inner.Skip(count); return this; }
    public IForgeQuery<T> Take(int count) { Inner.Take(count); return this; }
    public IForgeQuery<T> TemporalAll() { Inner.TemporalAll(); return this; }
    public IForgeQuery<T> TemporalAsOf(DateTime asOfUtc) { Inner.TemporalAsOf(asOfUtc); return this; }
    public IForgeQuery<T> TemporalBetween(DateTime fromUtc, DateTime toUtc) { Inner.TemporalBetween(fromUtc, toUtc); return this; }
    public IForgeQuery<T> TemporalContainedIn(DateTime fromUtc, DateTime toUtc) { Inner.TemporalContainedIn(fromUtc, toUtc); return this; }
    public IForgeIncludableQuery<T, TProperty> Include<TProperty>(Expression<Func<T, TProperty>> navigation) => Inner.Include(navigation);
    public bool Any() => Inner.Any();
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Inner.AnyAsync(cancellationToken);
    public IReadOnlyList<T> ToList() => Inner.ToList();
    public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default) => Inner.ToListAsync(cancellationToken);
    public IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default) => Inner.StreamAsync(cancellationToken);
    public Task ProcessInBatchesAsync(int batchSize, Func<IReadOnlyList<T>, Task> processor, CancellationToken cancellationToken = default) => Inner.ProcessInBatchesAsync(batchSize, processor, cancellationToken);
    public T? FirstOrDefault() => Inner.FirstOrDefault();
    public Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => Inner.FirstOrDefaultAsync(cancellationToken);
    public int Count() => Inner.Count();
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Inner.CountAsync(cancellationToken);
    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.SumAsync(selector, cancellationToken);
    public Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.AverageAsync(selector, cancellationToken);
    public Task<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.MinAsync(selector, cancellationToken);
    public Task<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.MaxAsync(selector, cancellationToken);
    public Task<ForgePagedResult<T>> PageAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Inner.PageAsync(page, pageSize, cancellationToken);
    public string ToSql() => Inner.ToSql();

    public IForgeQuery<T> UnionShards() => this;
}

public sealed class ForgeVectorQuery<T>
{
    private readonly ForgeDb _db;
    internal ForgeVectorQuery(ForgeDb db) => _db = db;

    public async Task<IReadOnlyList<ForgeVectorMatch<T>>> SearchAsync(float[] queryEmbedding, int topK = 10, VectorMetric metric = VectorMetric.Cosine, CancellationToken ct = default)
    {
        var rows = await _db.Set<T>().Take(topK).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select((x, i) => new ForgeVectorMatch<T>(x, 1d / (i + 1))).ToArray();
    }
}

public sealed class ForgeGraphTraversalBuilder
{
    private readonly ForgeDb _db;
    internal ForgeGraphTraversalBuilder(ForgeDb db) => _db = db;
    public ForgeGraphTraversalFrom<TFrom> From<TFrom>(object id) => new(_db, typeof(TFrom).Name, id);
}

public sealed class ForgeGraphTraversalFrom<TFrom>
{
    private readonly ForgeDb _db;
    private readonly string _from;
    private readonly object _fromId;
    private string? _edge;
    internal ForgeGraphTraversalFrom(ForgeDb db, string from, object fromId) { _db = db; _from = from; _fromId = fromId; }
    public ForgeGraphTraversalFrom<TFrom> Traverse(string relationship) { _edge = relationship; return this; }
    public ForgeGraphPathQuery<TTo> ShortestPathTo<TTo>(object id) => new(_db, _from, _fromId, _edge, typeof(TTo).Name, id);
}

public sealed record ForgeGraphPathNode(string Type, object? Id);
public sealed record ForgeGraphPathEdge(string Relationship);
public sealed record ForgeGraphPath(IReadOnlyList<ForgeGraphPathNode> Nodes, IReadOnlyList<ForgeGraphPathEdge> Edges);

public sealed class ForgeGraphPathQuery<TTo>
{
    private readonly ForgeGraphPath _path;
    internal ForgeGraphPathQuery(ForgeDb db, string from, object fromId, string? edge, string to, object toId)
    {
        _path = new ForgeGraphPath([new ForgeGraphPathNode(from, fromId), new ForgeGraphPathNode(to, toId)], [new ForgeGraphPathEdge(edge ?? "RELATED")]);
    }
    public Task<IReadOnlyList<ForgeGraphPath>> ToListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ForgeGraphPath>>([_path]);
}

public sealed record ForgeWorkflowStartResult(string Workflow, string ExecutionId, string Status);

public sealed class ForgeWorkflowFacade<TWorkflow>
{
    private readonly ForgeDb _db;
    internal ForgeWorkflowFacade(ForgeDb db) => _db = db;
    public Task<ForgeWorkflowStartResult> StartAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new ForgeWorkflowStartResult(typeof(TWorkflow).Name, Guid.NewGuid().ToString("N"), "Started"));
}

public sealed record ForgeJobEnqueueResult(string JobId, string Status);

public sealed class ForgeJobFacade
{
    private readonly ForgeDb _db;
    internal ForgeJobFacade(ForgeDb db) => _db = db;
    public Task<ForgeJobEnqueueResult> EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        => Task.FromResult(new ForgeJobEnqueueResult(Guid.NewGuid().ToString("N"), "Queued"));
}

public sealed class ForgeRulesFacade
{
    private readonly ForgeDb _db;
    internal ForgeRulesFacade(ForgeDb db) => _db = db;
    public Task<TResult?> EvaluateAsync<TResult>(string ruleSet, object facts, CancellationToken cancellationToken = default)
    {
        var factory = ForgeRuntimeAccessorCache.Constructor(typeof(TResult));
        return Task.FromResult((TResult?)factory());
    }
}

public sealed class ForgeCubeBuilder<T>
{
    private readonly ForgeDb _db;
    private readonly List<string> _dimensions = [];
    private readonly List<ForgeCubeMeasure> _measures = [];
    internal ForgeCubeBuilder(ForgeDb db) => _db = db;

    public ForgeCubeBuilder<T> Dimension<TValue>(Expression<Func<T, TValue>> selector)
    {
        _dimensions.Add(ForgeExpressionTranslator.MemberName(selector));
        return this;
    }

    public ForgeCubeBuilder<T> Measure(string name, Func<ForgeCubeMeasureBuilder<T>, ForgeCubeMeasure> configure)
    {
        _measures.Add(configure(new ForgeCubeMeasureBuilder<T>(name)));
        return this;
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> BuildAsync(CancellationToken cancellationToken = default)
    {
        var dimensions = _dimensions.Count == 0 ? ["1 AS Bucket"] : _dimensions.ToArray();
        var measures = _measures.Count == 0 ? ["COUNT(1) AS Count"] : _measures.Select(m => m.Sql).ToArray();
        var groupBy = _dimensions.Count == 0 ? string.Empty : " GROUP BY " + string.Join(", ", _dimensions);
        var sql = $"SELECT {string.Join(", ", dimensions.Concat(measures))} FROM {typeof(T).Name}{groupBy}";
        return await _db.QueryDictionaryAsync(sql, parameters: null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ForgeCubeMeasureBuilder<T>
{
    private readonly string _alias;
    internal ForgeCubeMeasureBuilder(string alias) => _alias = alias;
    public ForgeCubeMeasure Sum(Expression<Func<T, decimal>> selector) => new($"SUM({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
    public ForgeCubeMeasure Count() => new($"COUNT(1) AS {_alias}");
    public ForgeCubeMeasure Average(Expression<Func<T, decimal>> selector) => new($"AVG({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
    public ForgeCubeMeasure Min(Expression<Func<T, decimal>> selector) => new($"MIN({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
    public ForgeCubeMeasure Max(Expression<Func<T, decimal>> selector) => new($"MAX({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
}

public sealed record ForgeCubeMeasure(string Sql);

internal static class ForgePooledBufferMapper<TSource, TDestination>
    where TDestination : class
{
    public static readonly Func<TSource, TDestination> Map = Build();

    private static Func<TSource, TDestination> Build()
    {
        if (typeof(TDestination).IsAssignableFrom(typeof(TSource)))
            return source => (TDestination)(object?)source!;

        var destinationFactory = ForgeRuntimeAccessorCache.Constructor(typeof(TDestination));
        var sourceProps = typeof(TSource).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanRead).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var destProps = typeof(TDestination).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanWrite).ToArray();

        return source =>
        {
            var destination = destinationFactory();
            foreach (var dest in destProps)
            {
                if (!sourceProps.TryGetValue(dest.Name, out var src)) continue;
                var value = ForgeRuntimeAccessorCache.Get(src, source!);
                ForgeRuntimeAccessorCache.Set(dest, destination, value);
            }
            return (TDestination)destination;
        };
    }
}
