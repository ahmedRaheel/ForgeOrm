# Graph Insert / Update / Delete Full Sample Fix

Applied updates:

- Added `IncludeChildren`, `UseBulkWhenPossible`, `BatchSize`, and `Strategy` to `ForgeGraphInsertOptions<TParent,TDto>`.
- Added `UseSqlServerOpenJson()` to `ForgeGraphChildOptions<TDto,TChildEntity,TChildDto>`.
- Added safe OPENJSON fallback implementation so the API compiles and works until provider-specific JSON SQL generation is enabled.
- Added options-based `InsertGraphAsync<TParent,TDto,TKey>(dto, Action<ForgeGraphOptions>, ct)` for parent-only/options examples.
- Kept the existing graph-mapping API with `.Parent()`, `.Children()`, `.ForeignKey()`, `.UseSqlServerTvp()`, and `.UseSqlServerOpenJson()`.
- Added full sample endpoints for every insert/update/delete style under `/graph-persistence`:
  - single entity insert
  - DTO insert
  - many insert
  - entity auto graph insert
  - DTO graph auto insert
  - DTO graph TVP insert
  - DTO graph OPENJSON insert
  - expression parent-child graph insert
  - DTO factory graph insert
  - options-based parent-only graph insert
  - single parent update
  - graph update default
  - graph update delete-missing
  - graph update with `InsertUpdate`
  - graph update with `InsertUpdateDeleteMissing`
  - update by expression condition
  - update by SQL condition
  - parent-only delete
  - graph hard delete by id
  - graph hard delete by entity
  - graph soft delete by id
  - graph soft delete by entity
  - delete by expression condition

Note: .NET SDK is not available in this environment, so `dotnet build` could not be executed here.
