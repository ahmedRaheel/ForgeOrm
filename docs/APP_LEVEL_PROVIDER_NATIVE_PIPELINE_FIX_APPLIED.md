# App-Level Provider-Native Query Pipeline Fix Applied

This patch fixes the benchmark regression where `QueryFirstOrDefaultAsync` could still fall back to the generic `ForgePerformancePipeline` and allocate ~27 KB per call.

## What changed

- `ForgePerformancePipeline` now detects actual `SqlConnection` instances, not only provider-name metadata.
- All text query modes route to the SQL Server provider-native path when the connection is `SqlConnection`:
  - `QueryAsync`
  - typed `QueryAsync<T,TParameters>`
  - `FirstOrDefaultAsync`
  - `ExecuteAsync`
  - `ExecuteScalarAsync`
- `ForgeSqlServerProviderDirectHotPath.CanUse` now accepts provider implementations whose type name indicates SQL Server/SqlClient, not only `ProviderName == "SqlServer"`.
- Added provider-native overloads that execute against an existing `SqlConnection` and optional `SqlTransaction`, so connection-extension, transaction, and context APIs can share the same optimized SQL Server path.

## Why this matters

The earlier direct path could be bypassed when a factory/provider did not expose exactly `ProviderName = SqlServer`. That meant the benchmark could still execute the generic path despite the SQL Server hot-path code existing.

Now provider-native routing is based on the real runtime connection type as well.
