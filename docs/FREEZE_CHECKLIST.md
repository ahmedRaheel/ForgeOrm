# Pre-Release Freeze Checklist

## Build

- Clean solution
- Restore packages
- Build Debug
- Build Release

## Correctness

- Enum string storage works
- Enum numeric storage works when explicitly configured
- Nullable enum works
- Record DTO materialization works
- Graph insert works
- Graph update works
- Graph delete works
- Split query works
- CTE/temp/temporal APIs work from ForgeDb
- DataFrame/Pandas demo endpoint works

## Performance

- Query_By_Id allocation remains Dapper-level
- Search_Paged Take 10/50/100 remains Dapper-level
- No source-generator/analyzer logic appears in runtime hot path
- No new `ForgeDbContextFactory.Create()` inside benchmark method body

## API

- ForgeDb remains the single public access point
- No public ForgeSQL-only access required for features
- No duplicate Pandas extension method ambiguity
