# ForgeORM Performance Architecture Missing Links Fixed

Applied changes:

1. `ForgePerformancePipeline.QueryAsync` no longer delegates to `ForgeAdo.QueryAsync`. It now owns compiled plan lookup, parameter binding, command execution and materialization.
2. Added `ForgeCompiledQueryPlan<T>` and `ForgeCompiledExecutionPlanCache`.
3. Added cached parameter binder/layout compilation through `ForgeParameterBinderCompiler`.
4. Replaced hot-path SHA256 SQL fingerprints with `ForgeFastHash` non-cryptographic FNV-1a fingerprints.
5. Improved reader-shape cache key to include provider, target type, column names, CLR types, provider DB type names and nullable metadata.
6. Made `ForgeColumnOrdinalShapeCache` public so generated readers can use one normalized ordinal map instead of repeated per-column scans.
7. Tightened source-generator candidate selection. It now generates only for `[ForgeTable]`, `[ForgeGenerateMapper]`, `[ForgeDto]`, or `[ForgeProjection]` types instead of every public scalar POCO.
8. Fixed duplicate generated switch arm `_ => false` in generator output.
9. Routed `ForgeAdo.QueryAsync` through the central compiled execution pipeline so existing public APIs inherit the new path.

Remaining next-level work:

- Add provider-native bulk executors: SQL Server `SqlBulkCopy`, PostgreSQL `COPY`, MySQL bulk loader, Oracle array binding.
- Add BenchmarkDotNet perf gates in CI once SDK/build environment is available.
- Convert generated binders to provider-specific typed parameter APIs where possible.

10. Materializers remain resolved by actual reader shape at execution time. They are not cached directly on SQL-only plans, avoiding wrong reuse when the same SQL/provider returns a different projection shape.
