# ForgeORM Source-Generated Query Executor Hot Path Applied

This patch adds a full source-generated SQL Server first-row executor path instead of only generated metadata/materializers.

## What changed

- Added `IForgeSourceGeneratedAccessorProvider.TryExecuteSqlServerFirstOrDefaultAsync<T>()`.
- Added `ForgeSourceGeneratedRegistry.TryExecuteSqlServerFirstOrDefaultAsync<T>()`.
- `ForgeSqlServerProviderDirectHotPath.QueryFirstOrDefaultAsync<T>()` now checks the generated executor registry before generic command/materializer pipeline.
- `ForgeDb.QueryAsync`, `QueryFirstOrDefault`, `QueryFirstOrDefaultAsync`, and `ExecuteScalarAsync` now use the SQL Server direct provider path when the configured provider is SQL Server.
- Source generator now emits per-entity SQL Server first-row executors that:
  - create `SqlConnection`/`SqlCommand` directly,
  - bind the first SQL parameter directly,
  - use `CommandBehavior.SingleRow | SequentialAccess`,
  - materialize the entity inline without going through the generic reader-shape/materializer cache.
- Removed the unused prepared-command-pool lookup from the SQL Server text command path.

## Benchmark impact target

For `QueryFirstOrDefaultAsync<T>(sql, new { Id = id })`, this patch is intended to bypass:

- generic compiled plan lookup,
- generic binder resolution,
- generic reader shape materializer resolution,
- enterprise runtime context when disabled,
- list allocation.

The generated executor requires the source generator package to be referenced as an analyzer and the target type to be marked with `[ForgeTable]`, `[ForgeGenerateMapper]`, `[ForgeDto]`, or `[ForgeProjection]`.
