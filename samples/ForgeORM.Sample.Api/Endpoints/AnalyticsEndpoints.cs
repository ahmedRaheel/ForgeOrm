using ForgeORM.Analytics;
using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.DataFrame;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/analytics").WithTags("11 Analytics / DataFrame / Reports");

        group.MapGet("/window-functions/sql", (ForgeDbContext db) =>
        {
            var sql = db.Analytics<Order>()
                .From("Orders")
                .Select(x => x.Id)
                .Select(x => x.OrderNo)
                .Select(x => x.CustomerId)
                .Select(x => x.GrandTotal)
                .Select(x => x.CreatedAt)
                .RowNumber().PartitionBy(x => x.CustomerId).OrderByDescending(x => x.CreatedAt).As("RowNo")
                .Rank().PartitionBy(x => x.CustomerId).OrderByDescending(x => x.GrandTotal).As("RankNo")
                .DenseRank().PartitionBy(x => x.CustomerId).OrderByDescending(x => x.GrandTotal).As("DenseRankNo")
                .Count().PartitionBy(x => x.CustomerId).As("CustomerOrderCount")
                .Sum(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerTotalSales")
                .Avg(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerAverageSales")
                .Min(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerMinSale")
                .Max(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerMaxSale")
                .Lag(x => x.GrandTotal).PartitionBy(x => x.CustomerId).OrderBy(x => x.CreatedAt).As("PreviousOrderAmount")
                .Lead(x => x.GrandTotal).PartitionBy(x => x.CustomerId).OrderBy(x => x.CreatedAt).As("NextOrderAmount")
                .PercentRank().PartitionBy(x => x.CustomerId).OrderBy(x => x.GrandTotal).As("PercentRank")
                .CumeDist().PartitionBy(x => x.CustomerId).OrderBy(x => x.GrandTotal).As("CumeDist")
                .PercentileCont(x => x.GrandTotal, 0.5m).PartitionBy(x => x.CustomerId).As("MedianOrder")
                .PercentileCont(x => x.GrandTotal, 0.9m).PartitionBy(x => x.CustomerId).As("P90Order")
                .Sum(x => x.GrandTotal).PartitionBy(x => x.CustomerId).OrderBy(x => x.CreatedAt).RowsBetweenUnboundedPrecedingAndCurrentRow().As("RunningSales")
                .Render()
                .Sql;

            return Results.Ok(new { sql });
        });

        group.MapGet("/window-functions", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var result = await db.Analytics<Order>()
                .From("Orders")
                .Select(x => x.Id)
                .Select(x => x.OrderNo)
                .Select(x => x.CustomerId)
                .Select(x => x.GrandTotal)
                .Select(x => x.CreatedAt)
                .RowNumber().PartitionBy(x => x.CustomerId).OrderByDescending(x => x.CreatedAt).As("RowNo")
                .Rank().PartitionBy(x => x.CustomerId).OrderByDescending(x => x.GrandTotal).As("RankNo")
                .DenseRank().PartitionBy(x => x.CustomerId).OrderByDescending(x => x.GrandTotal).As("DenseRankNo")
                .Count().PartitionBy(x => x.CustomerId).As("CustomerOrderCount")
                .Sum(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerTotalSales")
                .Avg(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerAverageSales")
                .Min(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerMinSale")
                .Max(x => x.GrandTotal).PartitionBy(x => x.CustomerId).As("CustomerMaxSale")
                .Lag(x => x.GrandTotal).PartitionBy(x => x.CustomerId).OrderBy(x => x.CreatedAt).As("PreviousOrderAmount")
                .Lead(x => x.GrandTotal).PartitionBy(x => x.CustomerId).OrderBy(x => x.CreatedAt).As("NextOrderAmount")
                .Sum(x => x.GrandTotal).PartitionBy(x => x.CustomerId).OrderBy(x => x.CreatedAt).RowsBetweenUnboundedPrecedingAndCurrentRow().As("RunningSales")
                .ToDynamicListAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/pivot/orders", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var frame = await db.Frame<Order>()
                .FromSql("SELECT YEAR(CreatedAt) AS CreatedYear, Status, GrandTotal FROM dbo.Orders")
                .ToFrameAsync(ct);

            var pivot = frame.PivotTable(
                rows: "CreatedYear",
                columns: "Status",
                values: "GrandTotal",
                aggregate: ForgeAgg.Sum());

            return Results.Ok(pivot.Rows);
        });

        group.MapGet("/groupby/orders", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var frame = await db.Frame<Order>()
                .FromSql("SELECT Status, GrandTotal FROM dbo.Orders")
                .ToFrameAsync(ct);

            var summary = frame.GroupBy("Status")
                .Agg(
                    ForgeAggregation.Count(alias: "Orders"),
                    ForgeAggregation.Sum("GrandTotal", "Revenue"),
                    ForgeAggregation.Avg("GrandTotal", "AverageOrder"),
                    ForgeAggregation.Median("GrandTotal", "MedianOrder"));

            return Results.Ok(summary.Rows);
        });

        group.MapGet("/describe/orders", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var frame = await db.Frame<Order>()
                .FromSql("SELECT GrandTotal, TotalAmount FROM dbo.Orders")
                .ToFrameAsync(ct);

            return Results.Ok(frame.Describe("GrandTotal", "TotalAmount").Rows);
        });

        group.MapGet("/microsoft-dataframe/orders", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var frame = await db.Frame<Order>()
                .FromSql("SELECT TOP 20 Id, OrderNo, Status, GrandTotal, CreatedAt FROM dbo.Orders")
                .ToFrameAsync(ct);

            var microsoftFrame = frame.ToMicrosoftDataFrame();
            return Results.Ok(new
            {
                forgeRows = frame.RowCount,
                microsoftColumns = microsoftFrame.Columns.Select(c => c.Name).ToArray()
            });
        });

        //group.MapGet("/report/monthly-sales", async (ForgeDbContext db, CancellationToken ct) =>
        //{
        //    var report = await db.Report<Order>("MonthlySales")
        //        .From<Order>()
        //        .Dimension("Month", x => x.CreatedAt.Month)
        //        .Dimension("Status", x => x.Status)
        //        .Measure("Revenue", ForgeReportMeasure.Sum<Order>(x => x.GrandTotal))
        //        .Measure("Orders", ForgeReportMeasure.Count<Order>())
        //        .Filter(x => x.Status == OrderStatus.Paid)
        //        .Pivot("Status", "Month")
        //        .ExecuteAsync(ct);

        //    return Results.Ok(report);
        //});

        return app;
    }
}
