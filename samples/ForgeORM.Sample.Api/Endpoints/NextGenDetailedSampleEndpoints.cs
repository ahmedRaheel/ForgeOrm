using ForgeORM.Core.NextGen;

public static class NextGenDetailedSampleEndpoints
{
    public static IEndpointRouteBuilder MapNextGenDetailedSampleEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/nextgen-samples")
            .WithTags("101 NextGen Detailed Samples");

        MapMemory(root);
        MapSimd(root);
        MapParallel(root);
        MapConnections(root);
        MapDistributed(root);
        MapOlap(root);
        MapMachineLearning(root);
        MapLineage(root);
        MapSearch(root);
        MapVector(root);
        MapGraph(root);
        MapWorkflow(root);
        MapJobs(root);
        MapRules(root);
        MapHybrid(root);
        MapSync(root);
        MapRealtime(root);
        MapDesigner(root);
        MapAiAgents(root);
        MapEcosystem(root);

        return app;
    }

    private static void MapMemory(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/01-memory-allocation").WithTags("101.01 Memory Allocation");

        group.MapGet("/array-pool-buffer", () =>
        {
            using var buffer = new ForgeReusableBuffer<int>(10);

            for (var i = 0; i < buffer.Length; i++)
            {
                buffer.Span[i] = i + 1;
            }

            return Results.Ok(new
            {
                title = "ArrayPool reusable buffer",
                api = "using var buffer = new ForgeReusableBuffer<int>(10);",
                data = buffer.Span.ToArray()
            });
        });

        group.MapGet("/large-export-buffer-example", () =>
        {
            using var buffer = new ForgeReusableBuffer<byte>(1024 * 64);

            return Results.Ok(new
            {
                title = "Reusable 64KB export buffer",
                api = "new ForgeReusableBuffer<byte>(64 * 1024)",
                buffer.Length,
                reason = "Use this pattern for CSV, JSON, Excel, PDF and streaming exports to avoid repeated allocations."
            });
        });
    }

    private static void MapSimd(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/02-simd-vectorized").WithTags("101.02 SIMD Vectorized Analytics");

        group.MapGet("/sum", () =>
        {
            var values = Enumerable.Range(1, 1000)
                .Select(x => (float)x)
                .ToArray();

            return Results.Ok(new
            {
                title = "SIMD sum",
                api = "ForgeVectorizedMath.Sum(values)",
                sum = ForgeVectorizedMath.Sum(values)
            });
        });

        group.MapGet("/cosine-similarity", () =>
        {
            var a = new float[] { 0.1f, 0.2f, 0.3f };
            var b = new float[] { 0.1f, 0.2f, 0.35f };

            return Results.Ok(new
            {
                title = "Vector cosine similarity",
                api = "ForgeVectorizedMath.CosineSimilarity(a, b)",
                score = ForgeVectorizedMath.CosineSimilarity(a, b)
            });
        });
    }

    private static void MapParallel(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/03-parallel-query").WithTags("101.03 Parallel Query");

        group.MapGet("/parallel-processing", async (CancellationToken ct) =>
        {
            var rows = Enumerable.Range(1, 20);

            var result = await ForgeParallelQueryEngine.ExecuteAsync(
                rows,
                new ForgeParallelQueryOptions(MaxDegreeOfParallelism: 4),
                (x, _) => Task.FromResult(new { Id = x, Value = x * x }),
                ct);

            return Results.Ok(new
            {
                title = "Parallel query processing",
                api = "ForgeParallelQueryEngine.ExecuteAsync(... MaxDegreeOfParallelism: 4)",
                result
            });
        });
    }

    private static void MapConnections(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/04-intelligent-connections").WithTags("101.04 Intelligent Connections");

        group.MapGet("/pool-snapshot", () =>
        {
            var manager = new ForgeIntelligentConnectionManager();

            using var c1 = manager.Acquire();
            using var c2 = manager.Acquire();

            return Results.Ok(new
            {
                title = "Connection pool snapshot",
                api = "manager.Snapshot()",
                snapshot = manager.Snapshot()
            });
        });
    }

    private static void MapDistributed(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/05-distributed-systems").WithTags("101.05 Distributed Systems");

        group.MapGet("/distributed-lock", async (CancellationToken ct) =>
        {
            var locks = new ForgeDistributedLockManager();

            using var handle = await locks.AcquireAsync("customer:1001", ct);

            return Results.Ok(new
            {
                title = "Distributed lock foundation",
                api = "await locks.AcquireAsync(\"customer:1001\")",
                lockedResource = "customer:1001",
                status = "Locked and released safely"
            });
        });
    }

    private static void MapOlap(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/06-olap").WithTags("101.06 OLAP Engine");

        group.MapGet("/sales-cube", () =>
        {
            var cube = new ForgeOlapCube("SalesCube")
                .Dimension("Customer", "CustomerId")
                .Dimension("Status", "Status")
                .Dimension("Month", "CreatedMonth")
                .Measure("Revenue", "GrandTotal", "SUM")
                .Measure("Orders", "Id", "COUNT");

            return Results.Ok(new
            {
                title = "OLAP cube definition",
                api = "new ForgeOlapCube(\"SalesCube\").Dimension(...).Measure(...)",
                cube
            });
        });
    }

    private static void MapMachineLearning(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/07-machine-learning").WithTags("101.07 Machine Learning");

        group.MapGet("/moving-average", () =>
        {
            var revenue = new decimal[] { 100, 150, 200, 180, 260, 300 };

            return Results.Ok(new
            {
                title = "Moving average",
                api = "ForgeMachineLearning.MovingAverage(values, window: 3)",
                values = revenue,
                movingAverage = ForgeMachineLearning.MovingAverage(revenue, 3)
            });
        });

        group.MapGet("/anomaly-detection", () =>
        {
            var revenue = new decimal[] { 100, 120, 130, 3000, 140, 150 };

            return Results.Ok(new
            {
                title = "Anomaly detection",
                api = "ForgeMachineLearning.DetectAnomalies(values)",
                values = revenue,
                anomalies = ForgeMachineLearning.DetectAnomalies(revenue)
            });
        });
    }

    private static void MapLineage(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/08-lineage-governance").WithTags("101.08 Lineage Governance");

        group.MapPost("/record-access", (string entity, string user) =>
        {
            ForgeDataLineage.Record(new ForgeLineageEvent(
                Operation: "Read",
                Entity: entity,
                User: user,
                AtUtc: DateTimeOffset.UtcNow,
                Columns: ["Id", "Name", "CreatedAt"]));

            return Results.Ok(new
            {
                title = "Data lineage access record",
                api = "ForgeDataLineage.Record(...)",
                lineage = ForgeDataLineage.Snapshot()
            });
        });
    }

    private static void MapSearch(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/09-search").WithTags("101.09 Built-in Search");

        group.MapGet("/full-text", (string term) =>
        {
            var index = new ForgeSearchIndex();

            index.Upsert(new ForgeSearchDocument("1", "ForgeORM enterprise ORM and data platform", new Dictionary<string, object?> { ["Category"] = "ORM" }));
            index.Upsert(new ForgeSearchDocument("2", "Advanced analytics and reporting engine", new Dictionary<string, object?> { ["Category"] = "Analytics" }));

            return Results.Ok(new
            {
                title = "Built-in full-text search",
                api = "index.Search(term)",
                term,
                results = index.Search(term)
            });
        });
    }

    private static void MapVector(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/10-vector-db").WithTags("101.10 Vector Database");

        group.MapGet("/semantic-search", () =>
        {
            var index = new ForgeVectorIndex();

            index.Upsert(new ForgeVectorDocument("doc-1", "orders analytics", [0.1f, 0.3f, 0.5f]));
            index.Upsert(new ForgeVectorDocument("doc-2", "customer reports", [0.2f, 0.1f, 0.4f]));

            return Results.Ok(new
            {
                title = "Vector semantic search",
                api = "index.Search(embedding, topK: 5)",
                query = new float[] { 0.1f, 0.3f, 0.5f },
                results = index.Search([0.1f, 0.3f, 0.5f], 5)
            });
        });
    }

    private static void MapGraph(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/11-graph-db").WithTags("101.11 Graph Database");

        group.MapGet("/customer-orders", () =>
        {
            var graph = new ForgeGraphStore()
                .AddNode("customer-1", "Customer")
                .AddNode("order-1001", "Order")
                .AddNode("product-10", "Product")
                .AddEdge("customer-1", "order-1001", "PLACED")
                .AddEdge("order-1001", "product-10", "CONTAINS");

            return Results.Ok(new
            {
                title = "Graph traversal",
                api = "graph.Neighbors(\"customer-1\")",
                neighbors = graph.Neighbors("customer-1")
            });
        });
    }

    private static void MapWorkflow(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/12-workflow").WithTags("101.12 Workflow Engine");

        group.MapGet("/order-approval", () =>
        {
            var workflow = new ForgeWorkflowInstance()
                .AddStep("Create order", "Completed")
                .AddStep("Manager approval", "Pending")
                .AddStep("Finance approval", "Pending")
                .AddStep("Dispatch", "Blocked");

            return Results.Ok(new
            {
                title = "Order approval workflow",
                api = "new ForgeWorkflowInstance().AddStep(...)",
                workflow
            });
        });
    }

    private static void MapJobs(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/13-background-jobs").WithTags("101.13 Background Jobs");

        group.MapPost("/enqueue", (string name) =>
        {
            var queue = new ForgeBackgroundJobQueue();
            var job = queue.Enqueue(name);

            return Results.Ok(new
            {
                title = "Background job enqueue",
                api = "queue.Enqueue(name)",
                job,
                queue = queue.Snapshot()
            });
        });
    }

    private static void MapRules(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/14-rules").WithTags("101.14 Rule Engine");

        group.MapGet("/high-value-order", (decimal grandTotal) =>
        {
            var rule = new ForgeRule("HighValueOrder", "GrandTotal", ">", 10000m);

            return Results.Ok(new
            {
                title = "High value order rule",
                api = "ForgeRuleEngine.Evaluate(grandTotal, rule)",
                rule,
                grandTotal,
                passed = ForgeRuleEngine.Evaluate(grandTotal, rule)
            });
        });
    }

    private static void MapHybrid(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/15-schema-less-hybrid").WithTags("101.15 Schema-less Hybrid");

        group.MapGet("/dynamic-document", () =>
        {
            var document = new ForgeHybridDocument();

            document.Attributes["CustomerId"] = 1001;
            document.Attributes["RiskScore"] = 91.5m;
            document.Attributes["Tags"] = new[] { "vip", "priority" };

            return Results.Ok(new
            {
                title = "Hybrid dynamic document",
                api = "document.Attributes[\"RiskScore\"] = 91.5m",
                document
            });
        });
    }

    private static void MapSync(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/16-data-sync").WithTags("101.16 Data Synchronization");

        group.MapPost("/record-change", (string entity, string key, string operation) =>
        {
            var sync = new ForgeSyncEngine();

            sync.Record(new ForgeSyncChange(entity, key, operation, DateTimeOffset.UtcNow));

            return Results.Ok(new
            {
                title = "Record CDC/sync change",
                api = "sync.Record(new ForgeSyncChange(...))",
                pending = sync.Pending()
            });
        });
    }

    private static void MapRealtime(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/17-realtime").WithTags("101.17 Real-time Subscriptions");

        group.MapPost("/publish", (string channel, string payload) =>
        {
            var hub = new ForgeRealtimeHub();

            hub.Publish(channel, payload);

            return Results.Ok(new
            {
                title = "Realtime publish",
                api = "hub.Publish(channel, payload)",
                events = hub.Read(channel)
            });
        });
    }

    private static void MapDesigner(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/18-visual-designer").WithTags("101.18 Visual Designer Studio");

        group.MapGet("/query-artifact", () =>
        {
            var artifact = ForgeDesignerStudio.QueryDesigner(
                "HighValueOrders",
                "SELECT * FROM dbo.Orders WHERE GrandTotal > 10000");

            return Results.Ok(new
            {
                title = "Visual query designer artifact",
                api = "ForgeDesignerStudio.QueryDesigner(name, sql)",
                artifact
            });
        });
    }

    private static void MapAiAgents(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/19-ai-agents").WithTags("101.19 AI Coding Agents");

        group.MapGet("/generate-api", (string entity) =>
        {
            var result = ForgeAiCodingAgent.GenerateMinimalApi(entity);

            return Results.Ok(new
            {
                title = "AI coding agent API generator",
                api = "ForgeAiCodingAgent.GenerateMinimalApi(entity)",
                result
            });
        });
    }

    private static void MapEcosystem(RouteGroupBuilder root)
    {
        var group = root.MapGroup("/20-ecosystem").WithTags("101.20 Enterprise Ecosystem");

        group.MapGet("/snapshot", () =>
        {
            var snapshot = new ForgeEnterpriseEcosystemSnapshot(
                ForgeNextGenFeatureRegistry.All(),
                DateTimeOffset.UtcNow);

            return Results.Ok(new
            {
                title = "Complete enterprise ecosystem snapshot",
                api = "new ForgeEnterpriseEcosystemSnapshot(...)",
                snapshot
            });
        });
    }
}
