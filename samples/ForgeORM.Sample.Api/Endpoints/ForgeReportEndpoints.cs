using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;

public static class ForgeReportEndpoints
{
    public static IEndpointRouteBuilder MapForgeReportEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/forge-report")
            .WithTags("ForgeReport Easy API");

        group.MapGet("/top-customers/json", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomers")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

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

        group.MapGet("/top-customers/dictionary", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.Report<Order>("TopCustomers")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToDictionaryAsync(ct);

            return Results.Ok(rows);
        });

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

        group.MapGet("/sales-pivot/dictionary-expression", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.Report<Order>("SalesPivotExpression")
                .From("dbo.Orders")
                .PivotExpr(
                    row: x => x.CreatedAt.Year,
                    column: x => x.Status,
                    value: x => x.GrandTotal,
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/debug/sql-only", (
            ForgeDbContext db) =>
        {
            var sql = db.Report<Order>("DebugReport")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToSqlProjection();

            return Results.Ok(new
            {
                note = "SQL preview only. Use ToJsonAsync, ToDictionaryAsync, ToDataFrameAsync or ToCsvAsync for final output.",
                sql
            });
        });

        return app;
    }
}
