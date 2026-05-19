# Reporting Inference-Friendly Fix Applied

This update supports the intended clean syntax:

```csharp
var result = await db.Report<Order>("TopCustomersExpression")
    .From("dbo.Orders")
    .Dimension("CustomerId", x => x.CustomerId)
    .SumExpr(x => x.GrandTotal, "Revenue")
    .TopNSql("Revenue", 10)
    .ToJsonAsync(ct);
```

Also added:

```csharp
.PivotExpr(
    row: x => x.CreatedAt.Year,
    column: x => x.Status,
    value: x => x.GrandTotal,
    aggregate: "SUM",
    alias: "Revenue")
```

No repeated generic arguments are required because `db.Report<Order>()` already knows the entity type.
