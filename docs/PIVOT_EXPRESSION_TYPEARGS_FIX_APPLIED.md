# Pivot Expression Type Arguments Fix Applied

Problem:
```csharp
.Pivot<Order, int, OrderStatus, decimal>(...)
```

`db.Report<Order>()` already fixes the entity type, so the instance method only has 3 generic type arguments:

```csharp
.Pivot<int, OrderStatus, decimal>(...)
```

Preferred API added:

```csharp
.PivotExpr(
    row: x => x.CreatedAt.Year,
    column: x => x.Status,
    value: x => x.GrandTotal,
    aggregate: "SUM",
    alias: "Revenue")
```

Updated samples to use `.PivotExpr(...)`.
