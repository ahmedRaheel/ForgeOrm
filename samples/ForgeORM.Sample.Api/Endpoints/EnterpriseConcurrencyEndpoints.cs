using ForgeORM.Core;

public static class EnterpriseConcurrencyEndpoints
{
    public static IEndpointRouteBuilder MapEnterpriseConcurrencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/enterprise-concurrency")
            .WithTags("95 Enterprise Concurrency / Locking / Large Data");

        group.MapGet("/products/nolock", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products")
                .NoLock()
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(100)
                .Profile("Products-NoLock")
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/products/snapshot-read", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products")
                .SnapshotRead()
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(100)
                .Profile("Products-SnapshotRead")
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/products/readpast-queue", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products")
                .ReadPast()
                .RowLock()
                .Where(x => x.Price > 0)
                .OrderBy(x => x.Id)
                .Take(100)
                .Profile("Products-ReadPast-Queue")
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/products/update-lock", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products")
                .UpdateLock()
                .RowLock()
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .Take(10)
                .Profile("Products-UpdateLock")
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/products/read-consistency/{mode}", async (
            string mode,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var consistency = Enum.TryParse<ForgeReadConsistency>(
                    mode,
                    ignoreCase: true,
                    out var parsed)
                ? parsed
                : ForgeReadConsistency.Default;

            var rows = await db.Query<Product>()
                .From("dbo.Products")
                .WithReadConsistency(consistency)
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(50)
                .Profile($"Products-Consistency-{consistency}")
                .ToListAsync(ct);

            return Results.Ok(new
            {
                consistency = consistency.ToString(),
                rows
            });
        });

        group.MapGet("/products/throttled", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.QueryThrottledAsync<Product>(
                throttleName: "dashboard-products",
                maxConcurrency: 5,
                sql: """
SELECT TOP 100 *
FROM dbo.Products
WHERE Price > @MinPrice
ORDER BY Id DESC
""",
                parameters: new
                {
                    MinPrice = 100m
                },
                timeoutSeconds: 30,
                cancellationToken: ct);

            return Results.Ok(rows);
        });

        group.MapGet("/products/keyset", async (
            int? afterId,
            int take,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.QueryKeysetPageAsync<Product, int>(
                tableName: "dbo.Products",
                keyColumn: "Id",
                afterKey: afterId ?? 0,
                take: take <= 0 ? 100 : Math.Min(take, 1000),
                whereSql: "Price > @MinPrice",
                parameters: new
                {
                    MinPrice = 0m
                },
                orderDirection: "ASC",
                timeoutSeconds: 30,
                cancellationToken: ct);

            return Results.Ok(new
            {
                afterId,
                take,
                rows,
                nextAfterId = rows.Count == 0 ? afterId : rows[^1].Id
            });
        });

        group.MapPost("/products/process-batches", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var processed = await db.ProcessKeysetBatchesAsync<Product, int>(
                tableName: "dbo.Products",
                keyColumn: "Id",
                keySelector: x => x.Id,
                processBatch: (batch, token) =>
                {
                    // Put export, sync, cache warmup or analytics logic here.
                    return Task.CompletedTask;
                },
                batchSize: 1000,
                whereSql: "Id > @MinId",
                parameters: new
                {
                    MinId = 0
                },
                cancellationToken: ct);

            return Results.Ok(new
            {
                processed
            });
        });

        group.MapGet("/monitor/snapshot", () =>
        {
            return Results.Ok(ForgeEnterpriseQueryMonitor.Snapshot());
        });

        group.MapDelete("/monitor/clear", () =>
        {
            ForgeEnterpriseQueryMonitor.Clear();
            return Results.Ok(new { cleared = true });
        });

        return app;
    }
}
