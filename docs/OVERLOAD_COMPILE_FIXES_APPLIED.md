# Overload Compile Fixes Applied

Fixed:
- `.OptionalBetween(x => x.CreatedAt, from, to)` now has specific overloads for:
  - DateTime / DateTime?
  - DateTimeOffset / DateTimeOffset?
  - int / int?
  - long / long?
  - decimal / decimal?

Reporting extension methods were consolidated to avoid ambiguous overloads.

Supported report styles:
```csharp
.DimensionExpr(x => x.CustomerId)
.SumExpr(x => x.GrandTotal, "Revenue")

.DimensionExpr<Order, int>(x => x.CustomerId)
.SumExpr<Order, decimal>(x => x.GrandTotal, "Revenue")

.Dimension("CustomerId", x => x.CustomerId)
.Sum(x => x.GrandTotal, "Revenue")
```

Added sample endpoints:
- `/compile-fixed/search/orders-date-range`
- `/compile-fixed/report/top-customers-clean`
- `/compile-fixed/report/top-customers-explicit`
