using ForgeORM.Core;
using ForgeORM.Core.Search;

namespace ForgeORM.Sample.Api.Endpoints;

public static class ForgeAiCteTempSamples
{
    public static void MapForgeAiCteTempSamples(this IEndpointRouteBuilder app)
    {
        app.MapGet("/samples/ai/top-customers", async (ForgeDbContext db, string tenantId, CancellationToken ct) =>
        {
            var answer = await db.AI.QueryAsync(
                "Top 10 customers by revenue last month",
                options => options.TenantId = tenantId,
                ct);

            return Results.Ok(answer);
        })
        .WithTags("ForgeORM AI");


        app.MapGet("/samples/ai/optimize", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var suggestions = await db.AI.OptimizeAsync(
                "SELECT * FROM Orders WHERE CustomerId = @CustomerId ORDER BY CreatedAt DESC",
                ct);

            return Results.Ok(suggestions);
        })
        .WithTags("ForgeORM AI");

        app.MapGet("/samples/search/fulltext", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var products = await db.Search<Product>()
                .FullText("wireless keyboard")
                .Fuzzy()
                .Top(20)
                .ToListAsync(ct);

            return Results.Ok(products);
        })
        .WithTags("ForgeORM Search");

        app.MapGet("/samples/query/cte-orders", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var result = await db.Cte<Order>()
                .With("RecentOrders", q => q
                    .From<Order>()
                    .Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-30)))
                .From("RecentOrders")
                .ToListAsync(ct);

            return Results.Ok(result);
        })
        .WithTags("ForgeORM CTE");

        app.MapGet("/samples/query/temp-products", async (ForgeDbContext db, CancellationToken ct) =>
        {
            await db.TempTable<Product>("TempProducts")
                .FromQuery(q => q
                    .From<Product>()
                    .Where(x => x.Price > 100))
                .CreateAsync(ct);

            var result = await db.QueryAsync<Product>(
                "SELECT * FROM #TempProducts",
                ct);

            return Results.Ok(result);
        })
        .WithTags("ForgeORM Temp Tables");

        app.MapGet("/samples/query/cte-expression", (ForgeDbContext db) =>
        {
            var cte = db.Cte<Order>(
                "PaidOrders",
                x => x.Status == OrderStatus.Paid,
                x => x.Id,
                x => x.CustomerId,
                x => x.GrandTotal);

            var sql = db.Select<Order>()
                .WithCte(cte)
                .From("PaidOrders p")
                .Columns("p.CustomerId", "SUM(p.GrandTotal) AS Revenue")
                .GroupBy("p.CustomerId")
                .Render(db.Provider);

            return Results.Ok(new { sql.Sql, sql.Parameters });
        })
        .WithTags("ForgeORM CTE");

        app.MapGet("/samples/query/temp-expression", (ForgeDbContext db) =>
        {
            var script = db.TempTableFrom<Order>(
                "#PaidOrders",
                x => x.Status == OrderStatus.Paid,
                x => x.Id,
                x => x.CustomerId,
                x => x.GrandTotal);

            return Results.Ok(new { script.Sql, script.Parameters });
        })
        .WithTags("ForgeORM Temp Tables");
    }
}
