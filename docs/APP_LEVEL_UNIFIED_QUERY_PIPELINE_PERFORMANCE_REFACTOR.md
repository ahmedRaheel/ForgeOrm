# App-Level Unified Query Pipeline Performance Refactor

This patch avoids a one-method benchmark fix and applies the same execution model across the app-level query surface.

## Main changes

- `ForgeDb.Query`, `QueryAsync`, `QueryFirstOrDefault`, `QueryFirstOrDefaultAsync`, execute and scalar APIs now route through the unified performance pipeline or the provider-native SQL Server execution layer.
- `QueryFast*` is no longer a separate framework; it delegates to the same unified app-level query APIs.
- `DbConnection` extension methods no longer materialize a list for single-row APIs; `QuerySingle*` and `QueryFirst*` use the single-row pipeline.
- Transaction wrapper methods in `Support.cs` now route query/execute/scalar through `ForgePerformancePipeline` instead of `ForgeAdo`.
- SQL Server direct materializer cache now has a plan-keyed path by result type + SQL text, avoiding per-call reader shape string construction for stable SQL plans.
- SQL Server direct parameter binder no longer sorts/joins parameter names on every call; the parameter-name key is computed once in the SQL query plan.
- Removed unused hot-path `ForgePreparedCommandPool.GetOrAdd(...)` call from SQL Server direct text command creation.
- Existing no-attribute enum reader behavior is preserved.

## Goal

All high-level query APIs should use the same runtime flow:

`API -> Provider-native fast path when available -> ForgePerformancePipeline fallback -> compiled binder -> compiled materializer`

This avoids maintaining separate execution frameworks for benchmark methods vs normal app methods.
