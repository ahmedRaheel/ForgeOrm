# Framework-Wide Single-Row Execution Policy Applied

This update removes the previous separate `Find`/`GetById` fast executor framework and routes all single-row APIs through the same framework-level execution policy.

## Unified APIs

The following APIs now share the same policy path:

- `QueryFirstOrDefault`
- `QueryFirstOrDefaultAsync`
- `QuerySingleOrDefault`
- `QuerySingleOrDefaultAsync`
- `GetById`
- `GetByIdAsync`
- `Find`
- `FindAsync`
- `QueryFirstFast`
- `QueryFirstFastAsync`

## Key Changes

- Added typed `FirstOrDefault<T,TParameters>` and `SingleOrDefault<T,TParameters>` overloads in the framework execution policy.
- Added typed single-row overloads in `ForgePerformancePipeline`.
- Added `ForgeIdParameter<TKey>` as the provider-neutral primary-key parameter shape.
- Replaced the old `ForgeFastExecutor`/`ForgeFindPlan` side path.
- `GetById` and `Find` now use `Provider.BuildGetById(...)` only to get the provider-correct SQL and then execute through the same compiled policy as raw single-row queries.

## Resulting Flow

```text
GetById / Find / First / Single / Raw SQL
    -> ForgeFrameworkExecutionPolicy
    -> ForgePerformancePipeline
    -> ForgeCompiledExecutionPlanCache
    -> Binder
    -> Materializer
```

No public API now has a separate database-specific execution framework for single-row reads.
