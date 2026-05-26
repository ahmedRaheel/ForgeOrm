using ForgeORM.DataFrame;

namespace ForgeORM.Sample.Api.Endpoints;

public static class DataFramePandasCheatSheetEndpoints
{
    public static IEndpointRouteBuilder MapDataFramePandasCheatSheetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dataframe/pandas").WithTags("ForgeORM DataFrame - Pandas Cheat Sheet");

        group.MapGet("/demo", () =>
        {
            var frame = new ForgeDataFrame(new[]
            {
                new Dictionary<string, object?> { ["OrderId"] = 1, ["Customer"] = "Ali", ["Status"] = "Paid", ["Region"] = "South", ["GrandTotal"] = 1200m, ["OrderDate"] = "2026-01-01" },
                new Dictionary<string, object?> { ["OrderId"] = 2, ["Customer"] = "Sara", ["Status"] = "Pending", ["Region"] = "North", ["GrandTotal"] = 850m, ["OrderDate"] = "2026-01-02" },
                new Dictionary<string, object?> { ["OrderId"] = 3, ["Customer"] = "Ali", ["Status"] = "Paid", ["Region"] = "South", ["GrandTotal"] = 2400m, ["OrderDate"] = "2026-01-03" }
            });

            var cleaned = frame
                .StringTrim("Status")
                .ToNumeric("GrandTotal")
                .ToDateTime("OrderDate")
                .DateMonth("OrderDate", "Month")
                .WhereEquals("Status", "Paid")
                .WithColumn("Tax", r => Convert.ToDecimal(ForgeDataFrame.Get(r, "GrandTotal")) * 0.15m)
                .RollingMean("GrandTotal", 2, "RollingAvg")
                .ResetIndex();

            var report = cleaned
                .GroupBy("Customer")
                .Agg(
                    ForgeAggregation.Count(alias: "Orders"),
                    ForgeAggregation.Sum("GrandTotal", "Revenue"),
                    ForgeAggregation.Avg("GrandTotal", "AvgOrder"));

            return Results.Ok(new
            {
                Shape = cleaned.Shape(),
                Info = cleaned.Info(),
                DTypes = cleaned.DTypes().ToDictionaries(),
                Cleaned = cleaned.ToDictionaries(),
                Report = report.ToDictionaries(),
                ValueCounts = cleaned.ValueCounts("Region").ToDictionaries(),
                Pivot = cleaned.PivotTable("Customer", "Region", "GrandTotal", ForgeAgg.Sum()).ToDictionaries()
            });
        });

        return app;
    }
}
