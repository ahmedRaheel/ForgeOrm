# Overall Parameter + Performance Hardening Patch

This patch applies the provider-direct `@Id`/parameter-token safety net to the whole execution layer, not only `GetByIdAsync`.

## Applied

- Fixed the generic `ForgeAdo.CreateCommand` parameter safety net so every SQL token such as `@Id`, `@CustomerId`, `@ParentId`, and `@Ids` is checked before command execution.
- Removed the unreachable code bug in `EnsureReferencedSqlParametersAreBound` that prevented missing SQL tokens from being repaired after cached binders ran.
- Added pre-execution validation so ForgeORM throws a clear parameter-binding error before SQL Server raises `Must declare the scalar variable`.
- Kept scalar binding global: when SQL has a single scalar/token query, the scalar value is bound to every referenced token.
- Kept enumerable expansion support for `IN @Ids` and split-query scenarios.
- Removed duplicate `QueryDictionaryAsync` overload ambiguity and standardized calls to use named `cancellationToken`.
- Added SQL Server provider-direct dictionary query path for AI/report/frame/cube/dynamic rows.
- Added SQL Server direct command validation for all text query paths.
- Cached SQL parameter token extraction for provider-direct paths.

## Hot paths covered

- `GetByIdAsync`
- `QueryAsync`
- `QueryFirstOrDefaultAsync`
- `StreamAsync`
- `QueryDictionaryAsync`
- AI query/report/cube/dynamic analytics paths
- split-query and enumerable parameter scenarios

## Expected effect

This should eliminate remaining `Must declare the scalar variable "@Id"` style errors caused by command creation/binding mismatch, while keeping provider-direct optimization intact.
