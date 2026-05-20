# Frame Parallel + Expression API + Record Constructor MSIL Fix

Applied fixes:

- Added `ForgeFrameQuery<T>.EnableParallelExecution()`.
- Added `ForgeFrameQuery<T>.SetMaxDegreeOfParallelism(int)`.
- Added internal `ParallelExecutionEnabled` and `MaxParallelism` state.
- Added expression-based frame filtering: `db.Frame<Order>().Where(x => x.CreatedAt >= from)`.
- Added expression-based frame aggregation: `.SumAsync(x => x.GrandTotal, ct)`.
- Added typed vectorized frame API: `frame.Vectorized<Order>().Where(...).Sum(...)`.
- Updated MSIL materializer to support records / constructor DTOs.
- Materializer now supports constructor parameter binding by result column names.
- Parameterless constructor is still supported.
- Normal property-set materialization still works for class DTOs/entities.

Example:

```csharp
var revenue = await db.Frame<Order>()
    .Where(x => x.CreatedAt >= from)
    .Parallel()
    .MaxDegreeOfParallelism(8)
    .SumAsync(x => x.GrandTotal, ct);
```

Record DTO example:

```csharp
public sealed record OrderDto(int Id, string OrderNo, decimal GrandTotal);

var order = await db.GetByIdAsync<OrderDto>(id, ct);
```
