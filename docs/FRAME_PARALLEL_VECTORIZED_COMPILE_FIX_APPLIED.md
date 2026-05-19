# Frame Parallel + Vectorized Compile Fix Applied

This patch fixes the compile errors caused by `ForgeFrameHighPerformanceExtensions` calling missing members on `ForgeFrameQuery<T>`.

## Fixed

- Added `ForgeFrameQuery<T>.EnableParallelExecution()`.
- Added `ForgeFrameQuery<T>.SetMaxDegreeOfParallelism(int degree)`.
- Added internal frame query state:
  - `ParallelExecutionEnabled`
  - `MaxParallelism`
- Added expression-based frame filter:
  - `db.Frame<Order>().Where(x => x.CreatedAt >= from)`
- Added frame aggregate methods:
  - `SumAsync(x => x.GrandTotal, ct)`
  - `AverageAsync(x => x.GrandTotal, ct)`
  - `MinAsync(x => x.GrandTotal, ct)`
  - `MaxAsync(x => x.GrandTotal, ct)`
- Fixed vectorized chaining so this works:
  - `frame.Vectorized().Where("GrandTotal", ForgeVectorOperator.GreaterThan, 10000m).Sum("GrandTotal")`
- Added typed vectorized frame support:
  - `frame.Vectorized<Order>().Where(x => x.GrandTotal > 10000m).Sum(x => x.GrandTotal)`
- Updated sample endpoint `/frame/vectorized` to use `.Sum("GrandTotal")` instead of unclear `.Aggregate(...)`.

## Notes

No feature was removed. The old `Aggregate(string, ForgeAggregate)` method is still available for compatibility, but the sample now uses the clearer strong API.
