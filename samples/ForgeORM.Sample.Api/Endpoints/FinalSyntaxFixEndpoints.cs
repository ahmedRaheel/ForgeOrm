using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.Querying.Search;

public static class FinalSyntaxFixEndpoints
{
    public static IEndpointRouteBuilder MapFinalSyntaxFixEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/final-syntax-fix")
            .WithTags("Final Syntax Fix");

        group.MapGet("/sales-pivot/json", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("SalesPivot")
                .From("dbo.Orders")
                .Pivot(
                    row: "YEAR(CreatedAt)",
                    column: "Status",
                    value: "GrandTotal",
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/search/orders-between", async (
            DateTime? from,
            DateTime? to,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Search<Order>()
                .From("dbo.Orders")
                .OptionalBetween(x => x.CreatedAt, from, to)
                .OrderByDescending(x => x.CreatedAt)
                .Page(1, 50)
                .ToPagedAsync(ct);

            return Results.Ok(result);
        });

        return app;
    }
}
