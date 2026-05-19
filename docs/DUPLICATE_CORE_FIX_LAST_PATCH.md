# Duplicate Core Compile Fix

Fixed duplicate symbols introduced by the last API/performance patches:

- Renamed the query-builder result cache from `ForgeCompiledQueryCache` to `ForgeQueryResultCache`.
- Kept the execution compiled query-plan cache as `ForgeCompiledQueryCache`.
- Updated query-builder references to use `ForgeQueryResultCache.GetOrExecuteAsync(...)`.
- Renamed the CTE/temp-table private helper from `ResolveTableName<T>()` to `ResolveCteTableName<T>()` so it no longer conflicts with temporal helpers on the `ForgeDb` partial class.
- Removed the accidentally nested duplicate `ForgeOrm/` folder from the package to avoid opening/building the wrong duplicate solution.

No public API was changed.
