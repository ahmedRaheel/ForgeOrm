# Paged Execution Base Refactor Applied

This package applies paging performance fixes in the shared/base execution layer, not a single endpoint.

## Changed

- `ForgePerformancePipeline.Page<T>` added for true sync paged execution.
- `ForgePerformancePipeline.PageAsync<T>` now uses one opened connection for count + page query.
- `ForgeFrameworkExecutionPolicy.Page<T>` and `PageAsync<T>` route all public callers through the base pipeline.
- `ForgeDb.Page<T>` and `PageAsync<T>` now call the framework execution policy instead of creating separate count/page query paths.
- `EstimateCapacity(sql)` now detects `FETCH NEXT n`, `LIMIT n`, and `TOP n`, so paged lists are pre-sized to Take/PageSize instead of always defaulting to 32.

## Expected impact

- Less connection churn for paging.
- Fewer list resize allocations for Take 50/100.
- Shared cache path for count/page commands.
- No sync-over-async in paged base path.
