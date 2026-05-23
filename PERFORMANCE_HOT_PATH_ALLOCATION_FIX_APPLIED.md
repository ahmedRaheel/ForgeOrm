# ForgeORM hot-path allocation fix

This patch targets the regression visible in `QueryFirstOrDefaultAsync` / `Query_By_Id` benchmarks.

## Root causes fixed

1. Raw enum SQL normalization was executed for every query of a type that had enum properties, even when the SQL did not reference enum columns. This caused Regex/string allocations in simple `WHERE Id = @Id` queries.
2. Enterprise execution context was created before checking whether enterprise hooks/metrics were enabled.
3. SQL fingerprints were converted to strings on every cache lookup.
4. Reader shape/materializer resolution was repeated for the same compiled SQL plan.

## Applied changes

- Added `ForgeRawEnumSqlAnalyzer.RequiresNormalization<T>()` so enum SQL rewriting is decided once per compiled plan.
- Added `RequiresEnumNormalization` on `ForgeCompiledQueryPlan<T>`.
- `CreateCommand` now calls enum normalization only when the plan actually references an enum column.
- Enterprise context and stopwatch are now created only when `ForgeEnterpriseRuntime.IsEnabled` is true.
- Added `ForgeFastHash.HashSql()` and changed execution-plan/binder cache keys to use `ulong` hashes instead of allocating fingerprint strings per call.
- Cached `plan.Materializer` after first reader shape resolution so repeated same SQL/result queries skip reader-shape key allocation.

## Expected benchmark effect

`QueryFirstOrDefaultAsync<Order>(QueryById, new { Id = id })` should no longer pay for enum predicate rewriting, Regex, enterprise context construction, or repeated reader-shape key construction on the warm path.
