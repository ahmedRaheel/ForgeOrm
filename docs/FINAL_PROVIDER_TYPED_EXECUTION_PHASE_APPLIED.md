# Final Provider-Typed Execution Phase Applied

This update keeps ForgeORM's framework-level policy unified while reducing hot-path overhead across Query, First, Single, Scalar, Execute and Stream.

## Applied

- Source-generated SQL Server query executor registry changed from lock-per-call to copy-on-write volatile provider array.
- Direct command creation now uses provider-typed `SqlCommand` construction when the underlying connection is `SqlConnection`.
- Generic providers continue using the safe provider-neutral `DbConnection.CreateCommand()` path.
- Added `ForgeQueryWarmup` to prime direct execution plans before benchmark or service hot traffic.
- Existing compiled direct plans, typed parameter metadata, and generated-executor priority are preserved.

## Important benchmark note

Do not create configuration, DI, provider, or context objects inside the benchmark operation. Keep those in `GlobalSetup`; benchmark only the query call. Otherwise those allocations dominate the ORM result and hide query-engine improvements.

Recommended benchmark shape:

```csharp
[GlobalSetup]
public void Setup()
{
    _forge = ForgeDbContextFactory.Create();
    ForgeQueryWarmup.Precompile(BenchmarkSql.QueryById, new { Id = _settings.QueryOrderId });
}

[Benchmark]
public Task<Order?> ForgeORM_Query_By_Id()
    => _forge.QueryFirstOrDefaultAsync<Order>(BenchmarkSql.QueryById, new { Id = _settings.QueryOrderId });
```
