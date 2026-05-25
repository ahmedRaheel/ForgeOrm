using ForgeORM.Core.Enterprise;

public static class EnterpriseAdvancedFeaturePackEndpoints
{
    public static IEndpointRouteBuilder MapEnterpriseAdvancedFeaturePackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise-advanced")
            .WithTags("99 Enterprise Advanced Feature Pack 1-20");

        group.MapGet("/features", () => Results.Ok(ForgeEnterpriseFeatureRegistry.All()));

        group.MapGet("/1-distributed-query-execution", () =>
        {
            var router = new ForgeShardRouter();
            router.Register(new ForgeShardDescriptor("PK", "Server=pk-sql;Database=Forge;", Region: "Pakistan"));
            router.Register(new ForgeShardDescriptor("EU-Read", "Server=eu-read;Database=Forge;", IsReadReplica: true, Region: "Europe"));

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.DistributedQueryExecution),
                api = "db.Query<Order>().UseShard(\"PK\").UseReadReplica().UnionShards().ToListAsync(ct)",
                shards = router.All()
            });
        });

        group.MapGet("/2-distributed-cache", async (CancellationToken ct) =>
        {
            IForgeDistributedCache cache = new InMemoryForgeDistributedCache();
            await cache.SetAsync("products:featured", new[] { "P-100", "P-200" }, TimeSpan.FromMinutes(10), ct);

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.DistributedCache),
                api = "db.Query<Product>().CacheDistributed(TimeSpan.FromMinutes(5)).ToListAsync(ct)",
                cached = await cache.GetAsync<string[]>("products:featured", ct)
            });
        });

        group.MapPost("/3-query-plan-analysis", (QuerySqlRequest request) =>
            Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.QueryPlanAnalysis),
                api = "await db.Query<Order>().ExplainAsync(ct)",
                analysis = ForgeQueryPlanAnalyzer.Analyze(request.Sql)
            }));

        group.MapPost("/4-automatic-query-optimization", (QuerySqlRequest request) =>
            Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.AutomaticQueryOptimization),
                api = "db.Query<Order>().AutoOptimize().ToListAsync(ct)",
                original = request.Sql,
                optimized = ForgeAutomaticQueryOptimizer.Optimize(request.Sql)
            }));

        group.MapPost("/5-adaptive-query-execution", (AdaptiveQueryRequest request) =>
            Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.AdaptiveQueryExecution),
                api = "db.Query<Order>().Adaptive().ToListAsync(ct)",
                plan = ForgeAdaptiveExecutionPlanner.Plan(request.Sql, request.EstimatedRows)
            }));

        group.MapGet("/6-async-streaming", async (CancellationToken ct) =>
        {
            var count = 0;
            await foreach (var row in Enumerable.Range(1, 1000).ToAsyncStream(ct))
            {
                count++;
                if (count >= 10)
                {
                    break;
                }
            }

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.AsyncStreaming),
                api = "await foreach(var row in db.Query<Order>().StreamAsync(ct))",
                streamedRows = count
            });
        });

        group.MapGet("/7-columnar-analytics", () =>
        {
            var frame = new ForgeColumnarFrame();
            frame.AddColumn("Revenue", new object?[] { 100m, 250m, 400m });

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.ColumnarAnalytics),
                api = "frame.Column(\"Revenue\").VectorizedSum()",
                rows = frame.RowCount,
                revenue = frame.Sum("Revenue")
            });
        });

        group.MapGet("/8-materialized-query-cache", () =>
        {
            var cache = new ForgeMaterializedQueryCache();
            cache.Register(
                "DailySales",
                "SELECT CAST(CreatedAt AS date) Day, SUM(GrandTotal) Total FROM Orders GROUP BY CAST(CreatedAt AS date)",
                TimeSpan.FromMinutes(15));

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.MaterializedQueryCache),
                api = "db.Query<Order>().Materialize(\"DailySales\", TimeSpan.FromMinutes(15))",
                queries = cache.All()
            });
        });

        group.MapGet("/9-change-tracking-event-sourcing", () =>
        {
            var before = new SampleTrackedOrder(1, "Draft", 100m);
            var after = new SampleTrackedOrder(1, "Paid", 150m);

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.ChangeTrackingEventSourcing),
                api = "ForgeChangeTracker.Diff(before, after)",
                changes = ForgeChangeTracker.Diff(before, after)
            });
        });

        group.MapGet("/10-database-observability", () =>
        {
            ForgeDatabaseObservability.Record("slow_query_count", 2, "count");
            ForgeDatabaseObservability.Record("lock_wait_ms", 35, "milliseconds");

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.DatabaseObservability),
                api = "ForgeDatabaseObservability.Snapshot()",
                metrics = ForgeDatabaseObservability.Snapshot()
            });
        });

        group.MapGet("/11-opentelemetry", () =>
        {
            using var activity = ForgeOpenTelemetryBridge.StartQueryActivity("forgeorm.query", "tenant-a");

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.OpenTelemetryIntegration),
                api = "ForgeOpenTelemetryBridge.StartQueryActivity(\"forgeorm.query\", tenantId)",
                activityName = activity?.OperationName,
                traceId = activity?.TraceId.ToString()
            });
        });

        group.MapGet("/12-source-generators", () =>
        {
            var artifacts = new[]
            {
                new ForgeGeneratedArtifact("OrderReader.g.cs", "Reader", "Generated/OrderReader.g.cs", "Compiled reader for Order."),
                new ForgeGeneratedArtifact("OrderGraphPlan.g.cs", "GraphPlan", "Generated/OrderGraphPlan.g.cs", "Compiled graph plan for Order.")
            };

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.SourceGenerators),
                api = "ForgeORM.SourceGenerator emits compiled readers/mappers/plans",
                artifacts
            });
        });

        group.MapGet("/13-binary-protocol-optimizations", () =>
        {
            var policy = new ForgeBinaryOptimizationPolicy(
                UseBinaryImport: true,
                ReusePreparedStatements: true,
                UseStructuredParameters: true);

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.BinaryProtocolOptimizations),
                api = "options.UseBinaryImport().ReusePreparedStatements().UseStructuredParameters()",
                policy
            });
        });

        group.MapPost("/14-ai-native/to-sql", (AiPromptRequest request) =>
            Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.AiNativeFeatures),
                api = "await db.AI.QueryAsync(prompt, ct)",
                request.Prompt,
                sql = ForgeAiNative.ToSql(request.Prompt)
            }));

        group.MapGet("/15-advanced-transactions", () =>
        {
            var policy = new ForgeAdvancedTransactionPolicy(
                EnableRetry: true,
                EnableIdempotency: true,
                UseOutbox: true,
                MaxRetries: 3);

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.AdvancedTransactions),
                api = "await db.Transaction(policy).ExecuteAsync(...)",
                policy
            });
        });

        group.MapGet("/16-graphql-integration", () =>
        {
            var projection = new ForgeGraphQlProjection(
                "Order",
                ["Id", "OrderNo", "GrandTotal"],
                "GrandTotal > 100");

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.GraphQLIntegration),
                api = "builder.Services.AddForgeGraphQL()",
                projection
            });
        });

        group.MapGet("/17-data-virtualization", () =>
        {
            var registry = new ForgeDataVirtualizationRegistry();
            registry.Register(new ForgeVirtualDataSource("crm-sql", "SqlServer", "Server=crm;Database=CRM;"));
            registry.Register(new ForgeVirtualDataSource("billing-api", "HttpApi", "https://billing.example/api"));

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.DataVirtualization),
                api = "db.VirtualQuery().From(\"crm-sql\").Join(\"billing-api\")",
                sources = registry.Sources()
            });
        });

        group.MapGet("/18-time-series-optimization", () =>
            Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.TimeSeriesOptimization),
                api = "db.TimeSeries<Order>().BucketBy(\"day\").Aggregate(...)",
                dayBucket = ForgeTimeSeriesSql.BucketSql("CreatedAt", new ForgeTimeBucket("day", 1)),
                monthBucket = ForgeTimeSeriesSql.BucketSql("CreatedAt", new ForgeTimeBucket("month", 1))
            }));

        group.MapGet("/19-enterprise-migration-engine", () =>
            Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.EnterpriseMigrationEngine),
                api = "db.Migrations.Diff().GenerateZeroDowntimeScript()",
                operations = ForgeEnterpriseMigrationPlanner.PlanAddColumn("dbo.Orders", "TenantId", "nvarchar(64)", true)
            }));

        group.MapGet("/20-enterprise-admin-portal", () =>
        {
            var materialized = new ForgeMaterializedQueryCache();
            materialized.Register("SalesDashboard", "SELECT Status, SUM(GrandTotal) FROM Orders GROUP BY Status", TimeSpan.FromMinutes(5));

            var snapshot = new ForgeAdminDashboardSnapshot(
                ForgeEnterpriseFeatureRegistry.All(),
                ForgeDatabaseObservability.Snapshot(),
                materialized.All());

            return Results.Ok(new
            {
                feature = ForgeEnterpriseFeatureRegistry.Get(ForgeEnterpriseFeature.EnterpriseAdminPortal),
                api = "app.MapForgeAdminPortal()",
                snapshot
            });
        });

        return app;
    }

    private sealed record SampleTrackedOrder(int Id, string Status, decimal GrandTotal);
    private sealed record QuerySqlRequest(string Sql);
    private sealed record AdaptiveQueryRequest(string Sql, int EstimatedRows);
    private sealed record AiPromptRequest(string Prompt);
}
