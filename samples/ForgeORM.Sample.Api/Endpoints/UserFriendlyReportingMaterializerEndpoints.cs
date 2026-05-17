using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.Core.Materialization;

public static class UserFriendlyReportingMaterializerEndpoints
{
    public static IEndpointRouteBuilder MapUserFriendlyReportingMaterializerEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user-friendly-reporting")
            .WithTags("User Friendly Reporting Materializers");

        group.MapGet("/top-customers/json-sql", async (
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

        group.MapGet("/top-customers/dictionary-sql", async (
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

        group.MapGet("/top-customers/dataframe-sql", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var frame = await db.Report<Order>("TopCustomers")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToDataFrameAsync(ct);

            return Results.Ok(frame);
        });

        group.MapGet("/top-customers/csv-sql", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var csv = await db.Report<Order>("TopCustomers")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToCsvAsync(ct);

            return Results.Text(csv, "text/csv");
        });

        group.MapGet("/top-customers/json-expression", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersExpression")
                .From("dbo.Orders")
                .Dimension<Order, int>(x => x.CustomerId)
                .Sum<Order, decimal>(x => x.GrandTotal, "Revenue")
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

        group.MapGet("/sales-pivot/dictionary-sql", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.Report<Order>("SalesPivot")
                .From("dbo.Orders")
                .Pivot(
                    row: "YEAR(CreatedAt)",
                    column: "Status",
                    value: "GrandTotal",
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/sales-pivot/json-expression", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("SalesPivotExpression")
                .From("dbo.Orders")
                .Pivot<Order, int, OrderStatus, decimal>(
                    row: x => x.CreatedAt.Year,
                    column: x => x.Status,
                    value: x => x.GrandTotal,
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToJsonAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/debug/sql-only", (
            ForgeDbContext db) =>
        {
            var sql = db.Report<Order>("DebugSqlOnly")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToSqlProjection();

            return Results.Ok(new
            {
                note = "Use ToSqlProjection only for preview/debug. Use ToJsonAsync/ToDictionaryAsync for real execution.",
                sql
            });
        });

        return app;
    }
}
