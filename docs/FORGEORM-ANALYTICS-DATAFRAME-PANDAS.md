# ForgeORM AnalyticsFrame / Pandas-like API

ForgeORM now includes an optional analytics layer on top of the ORM engine.

## Foundation

- `ForgeORM.DataFrame` provides a pandas-like in-memory frame API.
- `ForgeORM.Analytics` provides SQL pushdown for window functions, pivot tables and percentile calculations.
- `Microsoft.Data.Analysis` is used as the optional Microsoft DataFrame bridge.

NuGet foundation confirmed: `Microsoft.Data.Analysis` 0.23.0 supports .NET 8 and has computed compatibility for .NET 10.

## SQL window function example

```csharp
var sql = db.Analytics<NdpStatement>()
    .From("[NDP].[vw_FBS_Stock_Statement]")
    .Select(x => x.CurrentFinancialStatYear, "Year")
    .PercentileCont(x => x.EbitdaToTotalIndebtedness, 0.75m)
        .PartitionBy(x => x.CurrentFinancialStatYear)
        .As("YearPercentile")
    .Count()
        .PartitionBy(x => x.CurrentFinancialStatYear)
        .As("YearSample")
    .PercentileCont(x => x.EbitdaToTotalIndebtedness, 0.75m)
        .OverAll()
        .As("OverallPercentile")
    .RowNumber()
        .PartitionBy(x => x.CurrentFinancialStatYear)
        .OrderBy(x => x.CurrentFinancialStatYear)
        .As("rn")
    .Render()
    .Sql;
```

## Pivot table

```csharp
var pivotSql = await db.Pivot<NdpStatement>()
    .From("[NDP].[vw_FBS_Stock_Statement]")
    .Rows(x => x.CurrentFinancialStatYear)
    .Columns(x => x.Sector)
    .Values(x => x.EbitdaToTotalIndebtedness)
    .Aggregate(ForgeSqlAggregate.Avg)
    .ToDynamicSqlServerPivotScriptAsync();
```

## DataFrame / pandas-like operations

```csharp
var frame = await db.Frame<Order>()
    .From("dbo.Orders")
    .ToFrameAsync();

var summary = frame
    .GroupBy("Status")
    .Agg(
        ForgeAggregation.Count(alias: "Orders"),
        ForgeAggregation.Sum("GrandTotal", "Revenue"),
        ForgeAggregation.Avg("GrandTotal", "AvgOrder"),
        ForgeAggregation.Median("GrandTotal", "MedianOrder"));

var pivot = frame.PivotTable(
    rows: "CreatedYear",
    columns: "Status",
    values: "GrandTotal",
    aggregate: ForgeAgg.Sum());

var microsoftDataFrame = frame.ToMicrosoftDataFrame();
```

## Phases implemented

### Phase 1
- Microsoft.Data.Analysis bridge
- SQL query to frame
- select/head/tail/sort/filter/fill/drop/drop duplicates
- describe

### Phase 2
- pivot table
- groupby aggregation
- percentile/median
- rolling aggregation
- SQL window functions
- SQL pushdown for percentile/row number/count

### Phase 3
- distributed frame plan contracts
- parquet adapter hook
- vector text adapter hook
- AI insights hook

### Phase 4
- extension points for GPU/vector/distributed execution without bringing heavy third-party dependencies into ForgeORM core.
