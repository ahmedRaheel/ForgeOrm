# Pivot and OptionalBetween Final Fix Applied

Fixed:
- Named string pivot syntax now works because ForgeReportBuilder<T> has an instance overload:

```csharp
.Pivot(
    row: "YEAR(CreatedAt)",
    column: "Status",
    value: "GrandTotal",
    aggregate: "SUM",
    alias: "Revenue")
```

Cause:
- C# prefers instance methods over extension methods.
- The old instance overload used parameter names rowExpression/columnExpression/valueExpression.
- Named arguments row/column/value were binding toward the expression overload and causing generic inference errors.

Fixed:
- OptionalBetween expression syntax now works:

```csharp
.OptionalBetween(x => x.CreatedAt, from, to)
```

by reducing overload ambiguity and using struct-constrained overloads for nullable ranges.
