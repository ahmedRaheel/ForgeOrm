using ForgeORM.Core;

namespace ForgeORM.Sample.Api;

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
