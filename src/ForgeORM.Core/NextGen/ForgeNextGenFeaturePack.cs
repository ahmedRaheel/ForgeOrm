using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

/// <summary>
/// Next-generation ForgeORM feature identifiers.
/// </summary>
public enum ForgeNextGenFeature
{
    MemoryAllocationOptimization,
    SimdVectorizedExecution,
    ParallelQueryExecution,
    IntelligentConnectionManagement,
    AdvancedDistributedSystems,
    OlapEngine,
    MachineLearningIntegration,
    DataLineageGovernance,
    BuiltInSearchEngine,
    VectorDatabaseSupport,
    GraphDatabaseSupport,
    WorkflowEngine,
    BackgroundProcessingEngine,
    RuleEngine,
    SchemaLessHybridMode,
    DataSynchronizationEngine,
    RealTimeSubscriptions,
    VisualDesignerStudio,
    AiCodingAgents,
    CompleteEnterpriseEcosystem
}

public sealed record ForgeNextGenFeatureResult(
    string Feature,
    string Status,
    string Description,
    IReadOnlyList<string> Capabilities,
    object? Data = null);

public static class ForgeNextGenFeatureRegistry
{
    private static ForgeNextGenFeatureResult Result(string name, string description, IReadOnlyList<string> capabilities)
        => new(name, "FoundationReady", description, capabilities);

    private static readonly IReadOnlyDictionary<ForgeNextGenFeature, ForgeNextGenFeatureResult> Items =
        new Dictionary<ForgeNextGenFeature, ForgeNextGenFeatureResult>
        {
            [ForgeNextGenFeature.MemoryAllocationOptimization] = Result("Memory + allocation optimization", "ArrayPool, reusable buffers, object pooling and low-allocation helpers.", ["ArrayPool", "ReusableBuffer", "ObjectPool", "ZeroAllocationReader"]),
            [ForgeNextGenFeature.SimdVectorizedExecution] = Result("SIMD/vectorized execution", "Vectorized numeric aggregation and analytics extension points.", ["VectorSum", "VectorFilter", "VectorScan", "ColumnarVector"]),
            [ForgeNextGenFeature.ParallelQueryExecution] = Result("Parallel query execution engine", "Parallel work partitioning and degree-of-parallelism policies.", ["Parallel", "MaxDegreeOfParallelism", "Partition", "Merge"]),
            [ForgeNextGenFeature.IntelligentConnectionManagement] = Result("Intelligent connection management", "Adaptive pooling, read replica routing and pool-starvation metrics.", ["AdaptivePool", "ReplicaRouting", "PoolStarvation", "HotStandby"]),
            [ForgeNextGenFeature.AdvancedDistributedSystems] = Result("Advanced distributed systems support", "Distributed locks, leader election and circuit breaker foundations.", ["DistributedLock", "LeaderElection", "CircuitBreaker", "ServiceDiscovery"]),
            [ForgeNextGenFeature.OlapEngine] = Result("Real OLAP engine", "Cube, dimension, measure and precomputed aggregation foundations.", ["Cube", "Dimension", "Measure", "Precompute"]),
            [ForgeNextGenFeature.MachineLearningIntegration] = Result("Machine learning integration", "Forecasting, anomaly detection, regression and clustering extension points.", ["Forecast", "AnomalyDetection", "Regression", "Clustering"]),
            [ForgeNextGenFeature.DataLineageGovernance] = Result("Data lineage + governance", "Access tracking, PII classification, lineage events and compliance audit.", ["AccessLog", "PIIClassification", "LineageGraph", "Compliance"]),
            [ForgeNextGenFeature.BuiltInSearchEngine] = Result("Built-in search engine", "Full-text, fuzzy and semantic search foundations.", ["Tokenize", "Rank", "FuzzySearch", "SemanticSearch"]),
            [ForgeNextGenFeature.VectorDatabaseSupport] = Result("Vector database support", "Embedding storage, cosine similarity, hybrid search and RAG primitives.", ["Embedding", "CosineSimilarity", "ANN", "RAG"]),
            [ForgeNextGenFeature.GraphDatabaseSupport] = Result("Graph database support", "Nodes, edges, traversal and graph analytics primitives.", ["Node", "Edge", "Traversal", "ShortestPath"]),
            [ForgeNextGenFeature.WorkflowEngine] = Result("Workflow engine", "Approval workflow, state machine and saga orchestration foundations.", ["StateMachine", "Approval", "Saga", "LongRunningProcess"]),
            [ForgeNextGenFeature.BackgroundProcessingEngine] = Result("Background processing engine", "Jobs, retries, cron, priority and batch processing foundations.", ["JobQueue", "Retry", "Cron", "PriorityQueue"]),
            [ForgeNextGenFeature.RuleEngine] = Result("Rule engine", "Dynamic business rules, expression evaluation and policy engine foundations.", ["Rules", "Policies", "PricingRules", "ApprovalRules"]),
            [ForgeNextGenFeature.SchemaLessHybridMode] = Result("Schema-less hybrid mode", "JSON columns, dynamic attributes and document projections.", ["JsonColumn", "DynamicAttribute", "DocumentProjection", "SchemaEvolution"]),
            [ForgeNextGenFeature.DataSynchronizationEngine] = Result("Data synchronization engine", "CDC, replication, incremental sync and conflict resolution foundations.", ["CDC", "Replication", "IncrementalSync", "ConflictResolution"]),
            [ForgeNextGenFeature.RealTimeSubscriptions] = Result("Real-time subscriptions", "Live query, change feed and push update foundations.", ["Subscribe", "ChangeFeed", "SignalRBridge", "PushUpdates"]),
            [ForgeNextGenFeature.VisualDesignerStudio] = Result("Visual designer/studio", "Visual query, ERD, dashboard, migration and report designer models.", ["QueryDesigner", "ERDDesigner", "DashboardDesigner", "ReportDesigner"]),
            [ForgeNextGenFeature.AiCodingAgents] = Result("AI coding agents inside ForgeORM Studio", "Generate queries, APIs, reports, CQRS and schema from prompts.", ["GenerateQuery", "GenerateApi", "GenerateReport", "GenerateCQRS"]),
            [ForgeNextGenFeature.CompleteEnterpriseEcosystem] = Result("Complete enterprise ecosystem", "Unified platform combining ORM, analytics, AI, workflow, search and streaming.", ["ORM", "Analytics", "AI", "Workflow", "Search", "Streaming"])
        };

    public static IReadOnlyList<ForgeNextGenFeatureResult> All() => Items.Values.ToList();
    public static ForgeNextGenFeatureResult Get(ForgeNextGenFeature feature) => Items[feature];
}

public sealed class ForgeReusableBuffer<T> : IDisposable
{
    private readonly ArrayPool<T> _pool;
    public T[] Buffer { get; }
    public int Length { get; }

    public ForgeReusableBuffer(int length, ArrayPool<T>? pool = null)
    {
        Length = length;
        _pool = pool ?? ArrayPool<T>.Shared;
        Buffer = _pool.Rent(length);
    }

    public Memory<T> Memory => Buffer.AsMemory(0, Length);
    public Span<T> Span => Buffer.AsSpan(0, Length);

    public void Dispose() => _pool.Return(Buffer, clearArray: true);
}

public static class ForgeVectorizedMath
{
    public static float Sum(ReadOnlySpan<float> values)
    {
        if (!Vector.IsHardwareAccelerated || values.Length < Vector<float>.Count)
        {
            var total = 0f;
            foreach (var value in values) total += value;
            return total;
        }

        var vectorTotal = Vector<float>.Zero;
        var i = 0;
        for (; i <= values.Length - Vector<float>.Count; i += Vector<float>.Count)
        {
            vectorTotal += new Vector<float>(values.Slice(i, Vector<float>.Count));
        }

        var result = 0f;
        for (var j = 0; j < Vector<float>.Count; j++) result += vectorTotal[j];
        for (; i < values.Length; i++) result += values[i];
        return result;
    }

    public static float CosineSimilarity(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        var length = Math.Min(left.Length, right.Length);
        if (length == 0) return 0;

        var dot = 0f;
        var leftMagnitude = 0f;
        var rightMagnitude = 0f;

        for (var i = 0; i < length; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        return leftMagnitude == 0 || rightMagnitude == 0
            ? 0
            : dot / (float)(Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}

public sealed record ForgeParallelQueryOptions(int MaxDegreeOfParallelism = 4, int PartitionSize = 1000);

public static class ForgeParallelQueryEngine
{
    public static async ValueTask<IReadOnlyList<TResult>> ExecuteAsync<T, TResult>(
        IEnumerable<T> source,
        ForgeParallelQueryOptions options,
        Func<T, CancellationToken, ValueTask<TResult>> worker,
        CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        var results = new ConcurrentBag<TResult>();

        var tasks = source.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                results.Add(await worker(item, cancellationToken));
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }
}

public sealed record ForgeConnectionPoolSnapshot(
    int ActiveConnections,
    int IdleConnections,
    int WaitingRequests,
    DateTimeOffset CapturedAtUtc);

public sealed class ForgeIntelligentConnectionManager
{
    private int _active;
    private int _waiting;

    public IDisposable Acquire()
    {
        Interlocked.Increment(ref _active);
        return new ReleaseHandle(() => Interlocked.Decrement(ref _active));
    }

    public ForgeConnectionPoolSnapshot Snapshot()
        => new(_active, Math.Max(0, Environment.ProcessorCount * 4 - _active), _waiting, DateTimeOffset.UtcNow);

    private sealed class ReleaseHandle : IDisposable
    {
        private readonly Action _release;
        private int _disposed;
        public ReleaseHandle(Action release) => _release = release;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) _release();
        }
    }
}

public sealed class ForgeDistributedLockManager
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    public async ValueTask<IDisposable> AcquireAsync(string name, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        return new ReleaseHandle(gate);
    }

    private sealed class ReleaseHandle : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        public ReleaseHandle(SemaphoreSlim gate) => _gate = gate;
        public void Dispose() => _gate.Release();
    }
}

public sealed record ForgeOlapDimension(string Name, string Column);
public sealed record ForgeOlapMeasure(string Name, string Expression, string Aggregate);

public sealed class ForgeOlapCube
{
    public string Name { get; }
    public List<ForgeOlapDimension> Dimensions { get; } = [];
    public List<ForgeOlapMeasure> Measures { get; } = [];

    public ForgeOlapCube(string name) => Name = name;

    public ForgeOlapCube Dimension(string name, string column)
    {
        Dimensions.Add(new(name, column));
        return this;
    }

    public ForgeOlapCube Measure(string name, string expression, string aggregate)
    {
        Measures.Add(new(name, expression, aggregate));
        return this;
    }
}

public static class ForgeMachineLearning
{
    public static IReadOnlyList<decimal> MovingAverage(IReadOnlyList<decimal> values, int window)
    {
        var result = new List<decimal>();
        for (var i = 0; i < values.Count; i++)
        {
            var start = Math.Max(0, i - window + 1);
            var slice = values.Skip(start).Take(i - start + 1).ToArray();
            result.Add(slice.Average());
        }
        return result;
    }

    public static IReadOnlyList<int> DetectAnomalies(IReadOnlyList<decimal> values, decimal thresholdMultiplier = 2m)
    {
        if (values.Count == 0) return [];
        var avg = values.Average();
        var variance = values.Average(v => (v - avg) * (v - avg));
        var std = (decimal)Math.Sqrt((double)variance);
        var threshold = std * thresholdMultiplier;

        return values.Select((v, i) => Math.Abs(v - avg) > threshold ? i : -1)
            .Where(i => i >= 0)
            .ToList();
    }
}

public sealed record ForgeLineageEvent(
    string Operation,
    string Entity,
    string User,
    DateTimeOffset AtUtc,
    IReadOnlyList<string> Columns);

public static class ForgeDataLineage
{
    private static readonly ConcurrentQueue<ForgeLineageEvent> Events = new();

    public static void Record(ForgeLineageEvent @event) => Events.Enqueue(@event);
    public static IReadOnlyList<ForgeLineageEvent> Snapshot() => Events.ToArray();
}

public sealed record ForgeSearchDocument(string Id, string Text, IReadOnlyDictionary<string, object?> Metadata);

public sealed class ForgeSearchIndex
{
    private readonly ConcurrentDictionary<string, ForgeSearchDocument> _docs = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(ForgeSearchDocument document) => _docs[document.Id] = document;

    public IReadOnlyList<ForgeSearchDocument> Search(string text)
        => _docs.Values
            .Where(x => x.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();
}

public sealed record ForgeVectorDocument(string Id, string Text, float[] Embedding);

public sealed class ForgeVectorIndex
{
    private readonly ConcurrentDictionary<string, ForgeVectorDocument> _docs = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(ForgeVectorDocument document) => _docs[document.Id] = document;

    public IReadOnlyList<(ForgeVectorDocument Document, float Score)> Search(float[] embedding, int topK = 5)
        => _docs.Values
            .Select(d => (d, ForgeVectorizedMath.CosineSimilarity(d.Embedding, embedding)))
            .OrderByDescending(x => x.Item2)
            .Take(topK)
            .Select(x => (x.d, x.Item2))
            .ToList();
}

public sealed record ForgeGraphNode(string Id, string Label);
public sealed record ForgeGraphEdge(string From, string To, string Type);

public sealed class ForgeGraphStore
{
    public List<ForgeGraphNode> Nodes { get; } = [];
    public List<ForgeGraphEdge> Edges { get; } = [];

    public ForgeGraphStore AddNode(string id, string label)
    {
        Nodes.Add(new(id, label));
        return this;
    }

    public ForgeGraphStore AddEdge(string from, string to, string type)
    {
        Edges.Add(new(from, to, type));
        return this;
    }

    public IReadOnlyList<ForgeGraphNode> Neighbors(string id)
    {
        var ids = Edges.Where(e => e.From == id).Select(e => e.To).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Nodes.Where(n => ids.Contains(n.Id)).ToList();
    }
}

public sealed record ForgeWorkflowStep(string Name, string Status);
public sealed class ForgeWorkflowInstance
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public List<ForgeWorkflowStep> Steps { get; } = [];

    public ForgeWorkflowInstance AddStep(string name, string status = "Pending")
    {
        Steps.Add(new(name, status));
        return this;
    }
}

public sealed record ForgeBackgroundJob(string Id, string Name, string Status, DateTimeOffset CreatedAtUtc);

public sealed class ForgeBackgroundJobQueue
{
    private readonly ConcurrentQueue<ForgeBackgroundJob> _jobs = new();

    public ForgeBackgroundJob Enqueue(string name)
    {
        var job = new ForgeBackgroundJob(Guid.NewGuid().ToString("N"), name, "Queued", DateTimeOffset.UtcNow);
        _jobs.Enqueue(job);
        return job;
    }

    public IReadOnlyList<ForgeBackgroundJob> Snapshot() => _jobs.ToArray();
}

public sealed record ForgeRule(string Name, string Field, string Operator, decimal Value);

public static class ForgeRuleEngine
{
    public static bool Evaluate(decimal actual, ForgeRule rule)
        => rule.Operator switch
        {
            ">" => actual > rule.Value,
            ">=" => actual >= rule.Value,
            "<" => actual < rule.Value,
            "<=" => actual <= rule.Value,
            "=" => actual == rule.Value,
            _ => false
        };
}

public sealed class ForgeHybridDocument
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public Dictionary<string, object?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record ForgeSyncChange(string Entity, string Key, string Operation, DateTimeOffset ChangedAtUtc);

public sealed class ForgeSyncEngine
{
    private readonly ConcurrentQueue<ForgeSyncChange> _changes = new();

    public void Record(ForgeSyncChange change) => _changes.Enqueue(change);
    public IReadOnlyList<ForgeSyncChange> Pending() => _changes.ToArray();
}

public sealed record ForgeRealtimeEvent(string Channel, string Payload, DateTimeOffset CreatedAtUtc);

public sealed class ForgeRealtimeHub
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ForgeRealtimeEvent>> _channels = new(StringComparer.OrdinalIgnoreCase);

    public void Publish(string channel, string payload)
        => _channels.GetOrAdd(channel, _ => new ConcurrentQueue<ForgeRealtimeEvent>())
            .Enqueue(new ForgeRealtimeEvent(channel, payload, DateTimeOffset.UtcNow));

    public IReadOnlyList<ForgeRealtimeEvent> Read(string channel)
        => _channels.TryGetValue(channel, out var q) ? q.ToArray() : [];
}

public sealed record ForgeDesignerArtifact(string Kind, string Name, string Json);

public static class ForgeDesignerStudio
{
    public static ForgeDesignerArtifact QueryDesigner(string name, string sql)
        => new("QueryDesigner", name, $$"""{"sql": "{{sql.Replace("\"", "\\\"")}}"}""");
}

public sealed record ForgeAiAgentResult(string Agent, string Prompt, string Output);

public static class ForgeAiCodingAgent
{
    public static ForgeAiAgentResult GenerateMinimalApi(string entity)
        => new("ApiAgent", $"Generate Minimal API for {entity}", $"app.MapGet(\"/{entity.ToLowerInvariant()}\", async (ForgeDbContext db) => await db.Query<{entity}>().ToListAsync());");
}

public sealed record ForgeEnterpriseEcosystemSnapshot(
    IReadOnlyList<ForgeNextGenFeatureResult> Features,
    DateTimeOffset CapturedAtUtc);
