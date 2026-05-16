# ForgeORM QueryAst Union, Group By, Having and Aggregate API

This pass adds SQL and expression-friendly alternatives for set operations and aggregate queries.

## Expression-first set operations

```csharp
var q = ForgeSql.Select<Product>()
    .Columns(p => p.Id, p => p.Code, p => p.Name)
    .Where(p => p.Price > 100)
    .UnionAll(next => next
        .Columns(p => p.Id, p => p.Code, p => p.Name)
        .Where(p => p.Price < 10))
    .Render(db.Provider);
```

## SQL set operations

```csharp
var q = ForgeSql.Select<Product>()
    .From("dbo.Products p")
    .ColumnsSql("p.Id", "p.Code", "p.Name")
    .UnionSql("SELECT Id, Code, Name FROM dbo.ArchivedProducts")
    .UnionAllSql("SELECT Id, Code, Name FROM dbo.LegacyProducts")
    .IntersectSql("SELECT Id, Code, Name FROM dbo.ActiveProducts")
    .ExceptSql("SELECT Id, Code, Name FROM dbo.BlockedProducts")
    .Render(db.Provider);
```

## Expression-first aggregates

```csharp
var q = ForgeSql.Select<Order>()
    .Columns(o => o.CustomerId)
    .Count("OrderCount")
    .Sum(o => o.TotalAmount, "TotalSales")
    .Average(o => o.TotalAmount, "AverageOrderValue")
    .Min(o => o.TotalAmount, "SmallestOrder")
    .Max(o => o.TotalAmount, "LargestOrder")
    .GroupBy(o => o.CustomerId)
    .HavingSum(o => o.TotalAmount, ">", 5000m)
    .Render(db.Provider);
```

## SQL aggregates

```csharp
var q = ForgeSql.Select<Order>()
    .From("dbo.Orders o")
    .ColumnsSql("o.CustomerId")
    .AggregateSql("COUNT(1)", "OrderCount")
    .AggregateSql("SUM(o.TotalAmount)", "TotalSales")
    .GroupBy("o.CustomerId")
    .HavingSql("SUM(o.TotalAmount) > @MinSales")
    .Render(db.Provider);
```

## Added methods

- `Union(...)`, `UnionSql(...)`
- `UnionAll(...)`, `UnionAllSql(...)`
- `Intersect(...)`, `IntersectSql(...)`
- `Except(...)`, `ExceptSql(...)`
- `Having(...)`, `HavingSql(...)`
- `Count(...)`, `Sum(...)`, `Average(...)`, `Min(...)`, `Max(...)`
- `AggregateSql(...)`
- `HavingCount(...)`, `HavingSum(...)`, `HavingAverage(...)`, `HavingMin(...)`, `HavingMax(...)`, `HavingAggregateSql(...)`
