# Duplicate class cleanup applied

Renamed feature-pack classes in `ForgeORM.QueryBuilder.Enterprise` to avoid conflicts/ambiguity with existing Core/Abstractions classes.

Renamed:
- `ForgeQueryProfiler` -> `ForgeEnterpriseQueryProfiler`
- `ForgeQueryProfile` -> `ForgeEnterpriseQueryProfile`
- `ForgeIndexSuggestionEngine` -> `ForgeEnterpriseIndexSuggestionEngine`
- `ForgeMemoryQueryCache` -> `ForgeEnterpriseMemoryQueryCache`
- `IForgeQueryCache` -> `IForgeEnterpriseQueryCache`
- `ForgePagedResult` -> `ForgeEnterprisePagedResult`
- `ForgeSavedQuery` -> `ForgeEnterpriseSavedQuery`
- `ForgeSavedQueryRegistry` -> `ForgeEnterpriseSavedQueryRegistry`

The main user-facing sample endpoints should continue to use `ForgeORM.Core.ForgeQueryProfiler`, `db.Query<T>()`, `Profile()`, and `Analyze()`.
