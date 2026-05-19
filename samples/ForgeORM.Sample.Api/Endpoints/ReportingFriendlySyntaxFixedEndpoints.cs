using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;

public static class ReportingFriendlySyntaxFixedEndpoints
{
    public static IEndpointRouteBuilder MapReportingFriendlySyntaxFixedEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reporting-friendly")
            .WithTags("Reporting Friendly Syntax Fixed");

        group.MapGet("/top-customers/json-expression", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersExpression")
                .From("dbo.Orders")
                .Dimension<Order>("CustomerId", x => x.CustomerId)
                .Sum<Order, decimal>(x => x.GrandTotal, "Revenue")
                .TopN("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/top-customers/json-expression-id", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersExpressionById")
                .From("dbo.Orders")
                .Dimension<Order>("OrderId", x => x.Id)
                .Sum<Order, decimal>(x => x.GrandTotal, "Revenue")
                .TopN("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/top-customers/json-sql", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersSql")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/sales-pivot/json-sql", async (
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

        group.MapGet("/sales-pivot/dictionary-expression", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("SalesPivotExpression")
                .From("dbo.Orders")
                .PivotExpr(
                    row: x => x.CreatedAt.Year,
                    column: x => x.Status,
                    value: x => x.GrandTotal,
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(result);
        });

        return app;
    }
}
