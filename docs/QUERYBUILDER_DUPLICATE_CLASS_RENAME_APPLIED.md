# QueryBuilder Duplicate Class Rename Applied

The sample/enterprise query-builder helper classes that conflicted with existing types were renamed inside `src/ForgeORM.Core/QueryBuilder/ForgeQueryBuilder.cs`:

- `ForgeQueryProfileEntry` -> `ForgeQueryBuilderProfileEntry`
- `ForgeQueryAnalysis` -> `ForgeQueryBuilderAnalysis`
- `ForgeQueryProfiler` -> `ForgeQueryBuilderProfiler`
- `ForgeIndexSuggestionEngine` -> `ForgeQueryBuilderIndexSuggestionEngine`

References in sample endpoints were updated accordingly. Existing abstraction-level classes remain untouched.
