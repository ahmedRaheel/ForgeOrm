using ForgeORM.DataFrame;

public static class PandasCompleteAnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapPandasCompleteAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dataframe/pandas-complete").WithTags("DataFrame Pandas Complete Analytics");

        group.MapGet("/demo", (ForgeORM.Core.ForgeDbContext db) =>
        {
            var frame = db.DataFrame(new Dictionary<string, IEnumerable<object?>>
            {
                ["Region"] = new object?[] { "North", "South", "North", "East", "South" },
                ["Revenue"] = new object?[] { 1200m, 700m, 1500m, null, 900m },
                ["Orders"] = new object?[] { 10, 5, 12, 2, 7 },
                ["Status"] = new object?[] { "Paid", "Draft", "Paid", "Cancelled", "Paid" },
                ["CreatedAt"] = new object?[] { "2026-05-01", "2026-05-02", "2026-05-03", "2026-05-04", "2026-05-05" }
            });

            var cleaned = frame.FillNa(0, "Revenue")
                .StrUpper("Region")
                .DtMonth("CreatedAt")
                .RollingMean("Revenue", 2, "RevenueRolling2")
                .CumSum("Revenue", "RevenueCumSum")
                .RankValues("Revenue", "RevenueRank", ascending: false);

            var grouped = cleaned.GroupBy("Region").Agg(
                ForgeAggregation.Count(alias: "Rows"),
                ForgeAggregation.Sum("Revenue", "TotalRevenue"),
                ForgeAggregation.Avg("Revenue", "AvgRevenue"));

            return Results.Ok(new
            {
                shape = frame.Shape(),
                dtypes = frame.DTypes(),
                info = frame.Info().Rows,
                describe = frame.Describe("Revenue", "Orders").Rows,
                valueCounts = frame.ValueCounts("Status").Rows,
                cleaned = cleaned.Rows,
                grouped = grouped.Rows,
                pivot = cleaned.PivotTable("Region", "Status", "Revenue", ForgeAgg.Sum()).Rows,
                melt = cleaned.Melt(new[] { "Region" }, new[] { "Revenue", "Orders" }).Rows,
                dummy = cleaned.GetDummies("Status", "Status_").Rows,
                markdown = grouped.ToMarkdown()
            });
        });

        return app;
    }
}
