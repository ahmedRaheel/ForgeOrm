using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapForgeReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reporting")
            .WithTags("36 Reporting Builder");

        group.MapGet("/pivot/sql", (ForgeDbContext db) =>
        {
            var sql = db.Report<Order>("SalesPivot")
                .From("dbo.Orders")
                .Pivot("YEAR(CreatedAt)", "Status", "GrandTotal", "SUM", "Revenue")
                .ToSql();

            return Results.Ok(new { sql });
        });

        group.MapGet("/unpivot/sql", (ForgeDbContext db) =>
        {
            var sql = db.Report<Order>("OrderMetricsUnpivot")
                .From("dbo.OrderMetricSnapshot")
                .Dimension("OrderId", "OrderId")
                .Unpivot("MetricName", "MetricValue", "GrandTotal", "TotalAmount")
                .ToSql();

            return Results.Ok(new { sql });
        });

        group.MapGet("/window/sql", (ForgeDbContext db) =>
        {
            var sql = db.Report<Order>("OrderWindowReport")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .RowNumber("RowNo", partitionBy: ["CustomerId"], orderBy: ["CreatedAt DESC"])
                .RollingAverage("GrandTotal", "CreatedAt", precedingRows: 6, alias: "RollingAverage", partitionBy: ["CustomerId"])
                .ToSql();

            return Results.Ok(new { sql });
        });

        group.MapGet("/percentile/sql", (ForgeDbContext db) =>
        {
            var sql = db.Report<Order>("OrderPercentileReport")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Percentile("GrandTotal", 0.5m, "MedianOrder", partitionBy: ["CustomerId"])
                .Percentile("GrandTotal", 0.9m, "P90Order", partitionBy: ["CustomerId"])
                .ToSql();

            return Results.Ok(new { sql });
        });

        group.MapGet("/top-n/sql", (ForgeDbContext db) =>
        {
            var sql = db.Report<Order>("TopCustomers")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToSql();

            return Results.Ok(new { sql });
        });

        group.MapGet("/drill/sql", (ForgeDbContext db) =>
        {
            var definition = db.Report<Order>("SalesDrillReport")
                .From("dbo.Orders")
                .Dimension("Year", "YEAR(CreatedAt)")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .DrillDown("Month", "MONTH(CreatedAt)")
                .DrillThrough("OrderDetails", "SELECT * FROM dbo.Orders WHERE CustomerId = @CustomerId")
                .Build();

            return Results.Ok(new
            {
                sql = ForgeReportSqlRenderer.Render(definition),
                drillDowns = definition.DrillDowns,
                drillThroughs = definition.DrillThroughs.Select(x => new { x.Name, x.Sql })
            });
        });

        group.MapGet("/export/csv", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var bytes = await db.Report<Order>("SalesCsv")
                .From("dbo.Orders")
                .Dimension("Status", "Status")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .ExportCsvAsync(ct);

            return Results.File(bytes, "text/csv", "sales-report.csv");
        });

        group.MapGet("/export/excel", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var bytes = await db.Report<Order>("SalesExcel")
                .From("dbo.Orders")
                .Dimension("Status", "Status")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .ExportExcelAsync("Sales", ct);

            return Results.File(bytes, "application/vnd.ms-excel", "sales-report.xls");
        });

        return app;
    }
}
