# ForgeORM Code Freeze Guardrails

This build is frozen for runtime execution architecture.

## Frozen areas

Do not redesign or rewrite these areas without benchmark proof:

- RuntimeEmit materializer pipeline
- SQL Server direct hot path
- parameter binding pipeline
- enum string/numeric handling
- query plan cache keys
- ForgeDb public access surface
- DataFrame/Pandas canonical API placement
- provider abstractions

## Allowed changes during freeze

Only these changes are allowed:

- compile fixes
- missing DataFrame method implementations
- provider-specific bug fixes
- enum mapping bug fixes
- documentation
- unit/integration tests
- profiler-confirmed micro-optimizations that keep allocation stable

## Not allowed

Do not reintroduce:

- source-generator projects
- analyzer packaging logic
- per-entity registration
- reflection setters in hot paths
- dictionary/object materializer lookup in loops
- string column lookup inside row loops
- LINQ in parameter/materializer hot paths
- generic DbDataReader path for SQL Server fast lane when a typed SqlDataReader path exists

## Performance acceptance gate

Before merging any performance-layer change, benchmark these cases:

- Query_By_Id
- Query_FirstOrDefault
- Search_Paged Take 10/50/100
- Enum string query
- Enum numeric query
- Record DTO materialization
- Split query
- Graph insert/update/delete

Do not merge if Query_By_Id allocation increases materially from the known Dapper-level baseline.
