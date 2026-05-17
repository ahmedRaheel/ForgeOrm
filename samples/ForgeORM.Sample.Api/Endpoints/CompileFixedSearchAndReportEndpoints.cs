using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.Core.Search;

public static class CompileFixedSearchAndReportEndpoints
{
    public static IEndpointRouteBuilder MapCompileFixedSearchAndReportEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/compile-fixed")
            .WithTags("Compile Fixed Search and Reporting");

        group.MapGet("/search/orders-date-range", async (
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

        group.MapGet("/report/top-customers-clean", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersClean")
                .From("dbo.Orders")
                .DimensionExpr(x => x.CustomerId)
                .SumExpr(x => x.GrandTotal, "Revenue")
                .TopNSql("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/report/top-customers-explicit", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersExplicit")
                .From("dbo.Orders")
                .DimensionExpr<Order, int>(x => x.CustomerId)
                .SumExpr<Order, decimal>(x => x.GrandTotal, "Revenue")
                .TopNSql("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

        return app;
    }
}
