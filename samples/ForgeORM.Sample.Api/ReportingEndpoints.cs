//using ForgeORM.Analytics.Reporting;
//using ForgeORM.Core;

//public static class ReportingEndpoints
//{
//    public static IEndpointRouteBuilder MapForgeReportingEndpoints(this IEndpointRouteBuilder app)
//    {
//        var group = app.MapGroup("/reporting")
//            .WithTags("36 Reporting Builder");

//        group.MapGet("/pivot/sql", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("SalesPivot")
//                .From("dbo.Orders")
//                .Pivot("YEAR(CreatedAt)", "Status", "GrandTotal", "SUM", "Revenue");

//            var sql = report.ToDictionaryProjection();
//            var rows = await report.ToDictionaryProjectionAsync(ct);

//            return Results.Ok(new { sql, rows });
//        });

//        group.MapGet("/pivot/expression", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("SalesPivotExpression")
//                .From("dbo.Orders")
//                .PivotByYear(x => x.CreatedAt, x => x.Status, x => x.GrandTotal, "SUM", "Revenue");

//            var sql = report.ToSql();
//            var rows = await report.ToListAsync(ct);

//            return Results.Ok(new { sql, rows });
//        });

//        group.MapGet("/unpivot/sql", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("OrderMetricsUnpivot")
//                .From("dbo.OrderMetricSnapshot")
//                .Dimension("OrderId", "OrderId")
//                .Unpivot("MetricName", "MetricValue", "GrandTotal", "TotalAmount");

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToDictionaryListAsync(ct)
//            });
//        });

//        group.MapGet("/unpivot/expression", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("OrderMetricsUnpivotExpression")
//                .From("dbo.Orders")
//                .Dimension("OrderId", x => x.Id)
//                .Unpivot("MetricName", "MetricValue", x => x.GrandTotal, x => x.TotalAmount);

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToListAsync(ct)
//            });
//        });

//        group.MapGet("/window/sql", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("OrderWindowReport")
//                .From("dbo.Orders")
//                .Dimension("CustomerId", "CustomerId")
//                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
//                .RowNumber("RowNo", partitionBy: ["CustomerId"], orderBy: ["CreatedAt DESC"])
//                .RollingAverage("GrandTotal", "CreatedAt", precedingRows: 6, alias: "RollingAverage", partitionBy: ["CustomerId"]);

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToListAsync(ct)
//            });
//        });

//        group.MapGet("/window/expression", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("OrderWindowExpression")
//                .From("dbo.Orders")
//                .Dimension("CustomerId", x => x.CustomerId)
//                .Sum(x => x.GrandTotal, "Revenue")
//                .RowNumber("RowNo", x => x.CustomerId, x => x.CreatedAt, descending: true)
//                .RollingAverage(x => x.GrandTotal, x => x.CreatedAt, precedingRows: 6, alias: "RollingAverage", partitionBy: x => x.CustomerId);

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToListAsync(ct)
//            });
//        });

//        group.MapGet("/percentile/sql", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("OrderPercentileReport")
//                .From("dbo.Orders")
//                .Dimension("CustomerId", "CustomerId")
//                .Percentile("GrandTotal", 0.5m, "MedianOrder", partitionBy: ["CustomerId"])
//                .Percentile("GrandTotal", 0.9m, "P90Order", partitionBy: ["CustomerId"]);

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToListAsync(ct)
//            });
//        });

//        group.MapGet("/percentile/expression", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("OrderPercentileExpression")
//                .From("dbo.Orders")
//                .Dimension("CustomerId", x => x.CustomerId)
//                .Percentile(x => x.GrandTotal, 0.5m, "MedianOrder", x => x.CustomerId)
//                .Percentile(x => x.GrandTotal, 0.9m, "P90Order", x => x.CustomerId);

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToListAsync(ct)
//            });
//        });

//        group.MapGet("/top-n/sql", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("TopCustomers")
//                .From("dbo.Orders")
//                .Dimension("CustomerId", "CustomerId")
//                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
//                .TopN("Revenue", 10);

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToListAsync(ct)
//            });
//        });

//        group.MapGet("/top-n/expression", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var report = db.Report<Order>("TopOrdersExpression")
//                .From("dbo.Orders")
//                .Dimension("CustomerId", x => x.CustomerId)
//                .Sum(x => x.GrandTotal, "Revenue")
//                .TopN("Revenue", 10);

//            return Results.Ok(new
//            {
//                sql = report.ToSql(),
//                rows = await report.ToListAsync(ct)
//            });
//        });

//        group.MapGet("/drill/sql", (ForgeDbContext db) =>
//        {
//            var definition = db.Report<Order>("SalesDrillReport")
//                .From("dbo.Orders")
//                .Dimension("Year", "YEAR(CreatedAt)")
//                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
//                .DrillDown("Month", "MONTH(CreatedAt)")
//                .DrillThrough("OrderDetails", "SELECT * FROM dbo.Orders WHERE CustomerId = @CustomerId")
//                .Build();

//            return Results.Ok(new
//            {
//                sql = ForgeReportSqlRenderer.Render(definition),
//                drillDowns = definition.DrillDowns,
//                drillThroughs = definition.DrillThroughs.Select(x => new { x.Name, x.Sql })
//            });
//        });

//        group.MapGet("/drill/expression", (ForgeDbContext db) =>
//        {
//            var definition = db.Report<Order>("SalesDrillExpression")
//                .From("dbo.Orders")
//                .Year("Year", x => x.CreatedAt)
//                .Sum(x => x.GrandTotal, "Revenue")
//                .DrillDown("Customer", x => x.CustomerId)
//                .DrillThrough("OrderDetails", "SELECT * FROM dbo.Orders WHERE CustomerId = @CustomerId")
//                .Build();

//            return Results.Ok(new
//            {
//                sql = ForgeReportSqlRenderer.Render(definition),
//                drillDowns = definition.DrillDowns,
//                drillThroughs = definition.DrillThroughs.Select(x => new { x.Name, x.Sql })
//            });
//        });

//        group.MapGet("/export/csv/sql", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var bytes = await db.Report<Order>("SalesCsv")
//                .From("dbo.Orders")
//                .Dimension("Status", "Status")
//                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
//                .ExportCsvAsync(ct);

//            return Results.File(bytes, "text/csv", "sales-report.csv");
//        });

//        group.MapGet("/export/csv/expression", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var bytes = await db.Report<Order>("SalesCsvExpression")
//                .From("dbo.Orders")
//                .Dimension("Status", x => x.Status)
//                .Sum(x => x.GrandTotal, "Revenue")
//                .ExportCsvAsync(ct);

//            return Results.File(bytes, "text/csv", "sales-report-expression.csv");
//        });

//        group.MapGet("/export/excel/sql", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var bytes = await db.Report<Order>("SalesExcel")
//                .From("dbo.Orders")
//                .Dimension("Status", "Status")
//                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
//                .ExportExcelAsync("Sales", ct);

//            return Results.File(bytes, "application/vnd.ms-excel", "sales-report.xls");
//        });

//        group.MapGet("/export/excel/expression", async (ForgeDbContext db, CancellationToken ct) =>
//        {
//            var bytes = await db.Report<Order>("SalesExcelExpression")
//                .From("dbo.Orders")
//                .Dimension("Status", x => x.Status)
//                .Sum(x => x.GrandTotal, "Revenue")
//                .ExportExcelAsync("SalesExpression", ct);

//            return Results.File(bytes, "application/vnd.ms-excel", "sales-report-expression.xls");
//        });

//        return app;
//    }
//}
