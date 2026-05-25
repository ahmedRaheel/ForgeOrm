# Performance Steps 2-9 Applied

This patch moves the performance fixes into the shared/base execution layer instead of a single query method.

## Applied

1. Added real synchronous methods to `ForgePerformancePipeline`:
   - `Query<T>`
   - `FirstOrDefault<T>`
   - `SingleOrDefault<T>`
   - `Execute`
   - `ExecuteScalar<T>`
   - typed-parameter sync overloads for query/single paths

2. Removed sync-over-async from base gateways:
   - `ForgeFrameworkExecutionPolicy`
   - `ForgeAdo`
   - `ForgeDbConnectionExtensions`

3. Materializer/plan cache now includes `ForgeSourceGeneratedRegistry.CompilationMode`, so `SourceGenerated` and `RuntimeEmit` do not reuse the same cached plan/materializer.

4. Parameter binder cache now includes compilation mode, preventing stale source-generated/runtime-emit binders from being reused after mode switches.

5. Single-row operations now execute through real sync reader paths and do not allocate a `List<T>`.

6. Query/list paths still use `SequentialAccess` and cached compiled materializers.

7. No `.AsTask().GetAwaiter().GetResult()` remains in the core base execution gateway files patched here.

## Files changed

- `src/ForgeORM.Core/Performance/ForgePerformancePipeline.cs`
- `src/ForgeORM.Core/Performance/ForgeCompiledExecutionPlan.cs`
- `src/ForgeORM.Core/Execution/ForgeFrameworkExecutionPolicy.cs`
- `src/ForgeORM.Core/ForgeAdo.cs`
- `src/ForgeORM.Core/ForgeDbConnectionExtensions.cs`
