# ForgeORM Enterprise Query and DataFrame Features

This update adds a full enterprise feature slice for read/query intelligence.

## Split query

```csharp
var plan = new ForgeSplitQueryBuilder<Order>()
    .From("dbo.Orders")
    .Where(x => x.CustomerId == customerId)
    .OrderBy(x => x.Id, ForgeSortDirection.Descending)
    .Page(page, pageSize)
    .Include("Items", "dbo.OrderItems", "Id", "OrderId")
    .Include("Payments", "dbo.Payments", "Id", "OrderId")
    .ToPlan();
```

## Dynamic search

```csharp
var query = ForgeSearchQuery.For<Order>()
    .From("dbo.Orders")
    .WhereIf(customerId.HasValue, x => x.CustomerId == customerId!.Value)
    .Contains(x => x.OrderNo, keyword)
    .Between(x => x.OrderDate, fromDate, toDate)
    .OrderBy(sortBy, ForgeSortDirection.Descending)
    .Page(page, pageSize)
    .ToSql();
```

## DataFrame analytics

```csharp
var report = frame
    .FillNull(0)
    .GroupByAggregate("Region",
        new ForgeFrameMeasure("Revenue", "TotalAmount", ForgeFrameAggregateKind.Sum),
        new ForgeFrameMeasure("Orders", "Id", ForgeFrameAggregateKind.Count))
    .SortBy("Revenue", descending: true);
```

## Pivot report

```csharp
var pivot = frame
    .Report()
    .Dimension("Region")
    .PivotBy("Month")
    .Measure("Revenue", "TotalAmount", ForgeFrameAggregateKind.Sum)
    .Execute();
```

## Query profiling and index suggestion

```csharp
var profiler = new ForgeQueryProfiler();
var (items, profile) = await profiler.ProfileAsync(
    "DashboardOrders",
    query,
    () => db.QueryAsync<Order>(query.Sql, query.Parameters));

var suggestedIndexes = ForgeIndexSuggestionEngine.Suggest(query);
```
