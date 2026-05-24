# Framework-wide Dapper-style direct execution applied

This update adds a provider-neutral direct execution lane behind the single ForgeORM framework policy. It is not a separate public API and is used by all supported query methods when the command shape is a simple scalar/anonymous parameter shape.

Applied across:

- `QueryAsync<T>`
- `QueryFirstOrDefaultAsync<T>`
- `QuerySingleOrDefaultAsync<T>`
- `ExecuteScalarAsync<T>`
- `ExecuteAsync`

Key changes:

- Added `ForgeDirectQueryExecutor` for low-allocation provider-neutral execution.
- Bypasses heavy command-plan/policy/enum-normalization layers when enterprise runtime is disabled and SQL shape is simple.
- Uses cached compiled parameter accessors for anonymous objects like `new { Id = id }`.
- Caches materializer delegates by provider/result/sql hash after first reader shape.
- Updated the actual `ForgeORM.SourceGenerators` analyzer project to emit provider-neutral query executors, not only the unused compatibility generator project.
- Fixed generated `typeof(T).FullName` comparison emission in the source generator.

Fallback remains the unified compiled execution pipeline for complex parameters, IN-list expansion, enterprise policies, graph/split queries, and stored procedures.
