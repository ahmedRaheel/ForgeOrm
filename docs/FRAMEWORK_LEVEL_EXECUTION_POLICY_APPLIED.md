# Framework-Level Execution Policy Applied

This patch removes database/method-specific public routing from ForgeORM public APIs.

## Rule

Every public API must flow through the same framework execution policy:

`ForgeDb / DbConnection extensions / fast APIs / stored procedures / scalar / execute / query / single-row / stream`

all route through:

`ForgeFrameworkExecutionPolicy -> ForgePerformancePipeline -> provider adapter/source-generated executor/runtime fallback`

## What changed

- Added `ForgeFrameworkExecutionPolicy` as the single public API gateway.
- Removed public `ForgeDb` branches that called `ForgeSqlServerProviderDirectHotPath` directly.
- Stored procedures, scalar, execute, query, single-row, stream, and fast APIs now use the same policy.
- `DbConnection` extension methods no longer call the old list-query path for single row operations.
- Provider-specific code remains internal and is selected behind `ForgePerformancePipeline` only.

## Design intent

SQL Server, PostgreSQL, MySQL, Oracle and any future provider share the same public execution framework.
Provider-specific optimization is allowed only behind adapters/registries, never as separate public API behavior.
