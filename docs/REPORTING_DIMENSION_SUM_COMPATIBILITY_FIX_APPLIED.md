# Reporting Dimension/Sum Compatibility Fix Applied

Added overloads so these all work:

```csharp
.DimensionExpr(x => x.CustomerId)
.DimensionExpr("CustomerId", x => x.CustomerId)
.DimensionExpr<Order, int>(x => x.CustomerId)

.SumExpr(x => x.GrandTotal, "Revenue")
.SumExpr<Order>(x => x.GrandTotal, "Revenue")
.SumExpr<Order, decimal>(x => x.GrandTotal, "Revenue")

.Dimension("CustomerId", x => x.CustomerId)
.Sum(x => x.GrandTotal, "Revenue")
```

Preferred style remains:

```csharp
db.Report<Order>("TopCustomers")
    .Dimension("CustomerId", x => x.CustomerId)
    .SumExpr(x => x.GrandTotal, "Revenue")
    .TopNSql("Revenue", 10)
    .ToJsonAsync(ct);
```
