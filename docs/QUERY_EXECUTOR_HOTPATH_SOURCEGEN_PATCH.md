# Query Executor Hot Path + Single Parameter Fast Binder Patch

Implemented a broader low-allocation query path instead of optimizing only one public method.

## Runtime changes

- Added `IForgeSqlServerQueryExecutorProvider` and `ForgeSqlServerQueryExecutorRegistry`.
- SQL Server direct query APIs now check registered whole-query executors before using the runtime pipeline.
- Added a single-parameter direct command path for all SQL Server text commands with one SQL parameter.
- The single-parameter fast path bypasses:
  - enumerable expansion checks beyond a cheap `IN` guard
  - query plan dictionary lookup
  - parameter-name sort/join
  - generic binder cache lookup
  - duplicate parameter validation loops
- Added an MSIL single-parameter binder cache for anonymous objects like `new { Id = id }`.
- The binder emits typed parameter calls for primitive values, string, decimal, Guid, bool, DateTime and enum.

## Why this matters

`QueryFirstOrDefaultAsync`, `QueryAsync`, scalar, execute and SQL Server connection-string paths now share the same optimized `CreateTextCommand` entry point. For the benchmark shape:

```csharp
await db.QueryFirstOrDefaultAsync<Order>(sql, new { Id = id });
```

ForgeORM now uses a direct one-parameter binder instead of the full runtime command-plan/binder path.

## Source-generated future path

The new `ForgeSqlServerQueryExecutorRegistry` enables the source generator to emit complete query executors that own:

- SQL text
- command behavior
- parameter binding
- reader materialization
- result shape

This is the path needed to remove generic runtime abstractions from the hottest queries while keeping the unified fallback pipeline for dynamic SQL.
