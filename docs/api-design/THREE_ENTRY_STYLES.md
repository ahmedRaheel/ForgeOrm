# ForgeORM Three Entry Styles

ForgeORM should be comfortable for every developer level.

Every major feature should expose three entry styles:

1. **Fluent / Query Builder**
2. **Raw SQL**
3. **Expression-based**

And every style should support the same terminal materializers:

```csharp
ToListAsync()
ToDictionaryAsync()
ToJsonAsync()
ToDataFrameAsync()
ToCsvAsync()
ToSql()
```

## Query Builder

```csharp
var rows = await db.Query<Product>()
    .From("dbo.Products")
    .Where(x => x.Price > 100)
    .OrderByDescending(x => x.Id)
    .Take(20)
    .ToListAsync(ct);
```

## Raw SQL

```csharp
var rows = await db.Sql(
        "SELECT TOP (20) * FROM dbo.Products WHERE Price > @MinPrice",
        new { MinPrice = 100m })
    .ToListAsync<Product>(ct);
```

## Expression

```csharp
var rows = await db.Expression<Product>()
    .From("dbo.Products")
    .Where(x => x.Price, ">", 100m)
    .OrderByDescending(x => x.Id)
    .Take(20)
    .ToListAsync(ct);
```

## Reporting

### Builder

```csharp
var result = await db.Report<Order>("TopCustomers")
    .From("dbo.Orders")
    .Dimension("CustomerId", "CustomerId")
    .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
    .TopN("Revenue", 10)
    .ToJsonAsync(ct);
```

### SQL

```csharp
var result = await db.Report<Order>("TopCustomersSql")
    .From("dbo.Orders")
    .DimensionSql("CustomerId", "CustomerId")
    .SumSql("GrandTotal", "Revenue")
    .TopNSql("Revenue", 10)
    .ToJsonAsync(ct);
```

### Expression

```csharp
var result = await db.Report<Order>("TopCustomersExpression")
    .From("dbo.Orders")
    .DimensionExpr<Order, int>(x => x.CustomerId)
    .SumExpr<Order, decimal>(x => x.GrandTotal, "Revenue")
    .TopNSql("Revenue", 10)
    .ToJsonAsync(ct);
```

## Product Rule

Users should never need to manually render SQL, execute SQL, read dictionaries, serialize JSON, or map dynamic report rows.

ForgeORM should do that inside terminal materializers.
