using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.Core.EntryStyles;

public static class ReportingExpressionFriendlyEndpoints
{
    public static IEndpointRouteBuilder MapReportingExpressionFriendlyEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/report-expression-friendly")
            .WithTags("Report Expression Friendly API");

        group.MapGet("/top-customers", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersExpression")
                .From("dbo.Orders")
                .Dimension("CustomerId", x => x.CustomerId)
                .SumExpr(x => x.GrandTotal, "Revenue")
                .TopNSql("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.Expression,
                result
            });
        });

        group.MapGet("/sales-pivot", async (
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
                .ToJsonAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.Expression,
                result
            });
        });

        return app;
    }
}
