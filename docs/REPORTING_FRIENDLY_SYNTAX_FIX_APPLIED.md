# Reporting Friendly Syntax Fix Applied

Fixed:
- .Dimension<Order>("CustomerId", x => x.CustomerId)
- .Sum<Order, decimal>(x => x.GrandTotal, "Revenue")
- .Pivot(row: "...", column: "...", value: "...", aggregate: "...", alias: "...")
- .Pivot<Order, int, OrderStatus, decimal>(...)
- .ToJsonAsync(ct)
- .ToDictionaryAsync(ct)

Important:
- Use .Dimension<Order>("CustomerId", x => x.CustomerId), not .Dimension<Order>("customerId", x => x.Id) if the intent is top customers.
- Pivot SQL overload supports named arguments row, column, value.
