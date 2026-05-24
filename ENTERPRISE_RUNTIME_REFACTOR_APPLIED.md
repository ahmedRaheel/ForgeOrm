# ForgeORM Enterprise Runtime Refactor Applied

This package applies a broad runtime architecture pass, not a single-method patch.

## Implemented

1. **Unified query framework**
   - `ForgeDb.Query/QueryAsync/First/Single/Scalar/Execute`
   - `DbConnection` extension APIs
   - transaction scope APIs
   - batch APIs
   now route through `ForgePerformancePipeline` instead of mixing `ForgeAdo`, direct SQL Server hot path, and performance pipeline.

2. **Hot-path allocation cleanup**
   - Enterprise context/stopwatch objects are no longer created when enterprise runtime is disabled.
   - Single-row path does not allocate `List<T>`.
   - Raw enum regex literal normalization is skipped unless the SQL contains a string literal.
   - Collection binding is centralized.

3. **Provider-native collection parameter abstraction**
   - `ForgeProviderNativeCollectionBinder` centralizes `IN @Ids` behavior.
   - PostgreSQL uses `ANY(@ids)` style rewrite.
   - SQL Server/MySQL/SQLite use safe expanded parameters.
   - This fixes split-query one-to-one/many-to-many duplicate `@Ids0` behavior at the shared binder level.

4. **Enum reader/writer direction**
   - Reader remains automatic: string values like `Paid` and numeric values both map to enum properties without requiring attributes.
   - Raw enum normalization is retained but guarded to reduce overhead.

5. **Centralized Batch and Unit of Work**
   - Added `db.TransactionAsync(tx => ...)`.
   - Added `db.Batch().Insert(...).Update(...).Delete(...).ExecuteAsync()`.
   - Both use the same compiled pipeline.

6. **Global policy/filter foundation**
   - Added `ForgeGlobalQueryPolicies` for tenant, soft delete, audit, and concurrency metadata.
   - This is the single central policy registry to be consumed by SQL builder, QueryAst, graph, and repository APIs.

7. **Compiled graph plan foundation**
   - Added `ForgeCompiledGraphPlanCache` to cache root metadata and child collection shape once per graph root type.

8. **SQL builder safety foundation**
   - Added `ForgeSqlSafety.GuardBuilderFragment` for builder-generated raw fragments.

## Remaining benchmarking note

The benchmark should now compare `QueryFirstOrDefaultAsync` against Dapper using the same raw SQL and parameters. If allocation is still high, the next specific target is ADO.NET command/parameter object pooling, because both SqlCommand and SqlParameter objects are provider allocations.
