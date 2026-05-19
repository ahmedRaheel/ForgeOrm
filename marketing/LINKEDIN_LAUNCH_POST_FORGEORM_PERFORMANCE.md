# LinkedIn Launch Post — ForgeORM Performance Update

I have been working on ForgeORM as an enterprise-grade .NET micro-ORM designed to combine Dapper-like speed with richer enterprise capabilities.

The latest performance update focuses on removing reflection from hot paths:

- MSIL DbDataReader materializers
- Source-generator-ready entity readers
- MSIL parameter binders
- Concurrent metadata and query-plan caches
- Sequential streaming readers
- Bulk and graph operation extension points
- Temporal helpers
- Provider optimization hooks
- Benchmarks against reflection baseline, Dapper-style and EF-style scenarios

ForgeORM is not just another ORM experiment. The goal is to provide a practical data access engine for real enterprise APIs: raw SQL, QueryAst, CTEs, pivot, stored procedures, table-valued parameters, graph inserts, CSV/JSON import, vector search, AI query helpers and dataframe-style analytics.

Next focus: publish benchmark numbers, production samples, documentation website, and provider-specific optimization guides.

#dotnet #csharp #opensource #microorm #sqlserver #postgresql #performance #softwarearchitecture
