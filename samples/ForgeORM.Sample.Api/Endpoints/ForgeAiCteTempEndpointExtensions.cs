using ForgeORM.Core;

public static class ForgeAiCteTempEndpointExtensions
{
    public static IEndpointRouteBuilder MapForgeAiCteTempSamples(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/forge-ai");

        group.MapGet("/ai/top-customers", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var answer = await db.AI.QueryAsync(
                "Top 10 customers by revenue last month"
               );

            return Results.Ok(answer);
        });

        group.MapGet("/cte/orders", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Cte<Order>()
                .With("RecentOrders", q => q
                    .From<Order>()
                    .Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-30)))
                .From("RecentOrders")
                .ToListAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/temp/products", async (
            ForgeDbContext db,
            CancellationToken ct) =>
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
        });

        return app;
    }
}