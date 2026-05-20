using System.Buffers;
using ForgeORM.Core;
using ForgeORM.Core.Search;
using ForgeORM.DataFrame;

public static class AdvancedDbSurfaceEndpoints
{
    public static IEndpointRouteBuilder MapAdvancedDbSurfaceSamples(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/advanced-db-surface").WithTags("Advanced DB Surface");

        group.MapGet("/search/fulltext", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var products = await db.Search<Product>()
                .FullText("wireless keyboard")
                .Fuzzy()
                .Top(20)
                .ToListAsync(ct);

            return Results.Ok(products);
        });

        group.MapGet("/vector/products", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var queryEmbedding = new[] { 0.11f, 0.42f, 0.73f };
            var matches = await db.Vector<Product>()
                .SearchAsync(queryEmbedding, topK: 10, metric: VectorMetric.Cosine, ct);

            return Results.Ok(matches);
        });

        group.MapGet("/graph/path", async (ForgeDbContext db, int customerId, int productId, CancellationToken ct) =>
        {
            var path = await db.Graph()
                .From<Customer>(customerId)
                .Traverse("PLACED_ORDER")
                .ShortestPathTo<Product>(productId)
                .ToListAsync(ct);

            return Results.Ok(path);
        });

        group.MapGet("/ai/optimize", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var suggestions = await db.AI.OptimizeAsync(
                "SELECT * FROM Orders WHERE CustomerId = @CustomerId ORDER BY CreatedAt DESC",
                ct);

            return Results.Ok(suggestions);
        });

        group.MapPost("/workflow/order-approval", async (ForgeDbContext db, int orderId, CancellationToken ct) =>
        {
            await db.Workflow<OrderApproval>()
                .StartAsync(new OrderApprovalRequest(orderId), ct);

            await db.Jobs.EnqueueAsync(new RecalculateCustomerScore(orderId), ct);

            return Results.Ok(new { started = true, orderId });
        });

        group.MapGet("/rules/pricing", async (ForgeDbContext db, int id, string tier, CancellationToken ct) =>
        {
            var price = await db.Rules()
                .EvaluateAsync<PriceRuleResult>("PricingRules", new { ProductId = id, CustomerTier = tier }, ct);

            return Results.Ok(price);
        });

        group.MapGet("/cube/revenue", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var cube = await db.Cube<Order>()
                .Dimension(x => x.CustomerId)
                .Dimension(x => x.Status)
                .Measure("Revenue", x => x.Sum(o => o.GrandTotal))
                .BuildAsync(ct);

            return Results.Ok(cube);
        });

        group.MapGet("/frame/top-customers", async (ForgeDbContext db, DateTimeOffset? from, CancellationToken ct) =>
        {
            var frame = await db.Frame<Order>()
                .Where(x => x.CreatedAt >= (from ?? DateTimeOffset.UtcNow.AddDays(-30)))
                .ToFrameAsync(ct);

            var report = frame
                .GroupBy("CustomerId")
                .Sum("GrandTotal")
                .SortByDescending("GrandTotal")
                .Take(20);

            return Results.Ok(report.Rows);
        });

        group.MapGet("/frame/parallel-revenue", async (ForgeDbContext db, DateTimeOffset? from, CancellationToken ct) =>
        {
            var revenue = await db.Frame<Order>()
                .Where(x => x.CreatedAt >= (from ?? DateTimeOffset.UtcNow.AddDays(-30)))
                .Parallel()
                .MaxDegreeOfParallelism(8)
                .SumAsync(x => x.GrandTotal, ct);

            return Results.Ok(new { revenue });
        });

        group.MapGet("/frame/vectorized", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var frame = await db.Frame<Order>().ToFrameAsync(ct);
            var filtered = frame.Vectorized()
                .Where("GrandTotal", ForgeVectorOperator.GreaterThan, 10000m)
                ;

            return Results.Ok(new { sum = filtered });
        });

        group.MapGet("/shards/eu", async (ForgeDbContext db, int tenantId, CancellationToken ct) =>
        {
            var rows = await db.Set<Order>()
                .UseShard("EU")
               // .Where(x => x.TenantId == tenantId)
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/shards/union", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var allRegions = await db.Set<Order>()
                .UseShard("EU")
                .UseShard("US")
                .UnionShards()
                .ToListAsync(ct);

            return Results.Ok(allRegions);
        });

        group.MapGet("/read-into", async (ForgeDbContext db, DateTimeOffset? from, CancellationToken ct) =>
        {
            const int batchSize = 256;
            var buffer = ArrayPool<OrderDto>.Shared.Rent(batchSize);
            try
            {
                var count = await db.Set<Order>()
                    .Where(x => x.CreatedAt >= (from ?? DateTimeOffset.UtcNow.AddDays(-30)))
                    .ReadIntoAsync(buffer, ct);

                return Results.Ok(new { count });
            }
            finally
            {
                ArrayPool<OrderDto>.Shared.Return(buffer, clearArray: true);
            }
        });

        return app;
    }
}

public sealed record OrderApproval;
public sealed record OrderApprovalRequest(int OrderId);
public sealed record RecalculateCustomerScore(int CustomerId);
public sealed record PriceRuleResult(decimal Price = 0m, string Currency = "USD", string Rule = "Default");
public sealed class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
