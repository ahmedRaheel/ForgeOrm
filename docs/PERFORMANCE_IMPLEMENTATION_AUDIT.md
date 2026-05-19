# ForgeORM Performance Implementation Audit

## Applied in this patch

- RuntimeEmit mode retained through `ForgeORM.RuntimeEmit` and `ForgeRuntimeAccessorCache`.
- SourceGenerated mode changed from a placeholder package into a Roslyn incremental generator project.
- Generated code now supports compile-time registration for:
  - DbDataReader readers
  - DbCommand binders
  - insert SQL strings
  - graph metadata strings
  - entity map/table/column names
- Column-name based reader binding is explicit through `ForgeReaderShapeCache` and reader-shape cache keys.
- Provider-specific executor abstraction added through `IForgeProviderExecutor`.
- Provider hot-path executor classes added for:
  - SqlServer
  - PostgreSql
  - MySql
  - Oracle
- Compiled query cache added through `ForgeCompiledQueryCache`.
- Public `InsertFast` / `InsertFastAsync` API removed. Existing `Insert` / `InsertAsync` route to the compiled insert path.
- `GetByIdAsync<T>(id)` scalar `@Id` parameter binding remains globally fixed in `ForgeAdo.BindScalarParameter`.
- Async streaming executor path added with `IAsyncEnumerable<T>` and `CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection`.
- Graph bulk write plan model added for TVP / SqlBulkCopy / temp-table merge strategy selection.
- Benchmark gate manifest added for:
  - query one row
  - query list
  - insert one
  - bulk insert
  - graph insert
  - get by id
  - page
  - stream

## Important note

Reflection is now intended only for cold-path plan creation, metadata discovery, or non-execution tooling. Runtime row materialization, parameter binding, property get/set in graph/insert hot paths, and normal CRUD/query execution use cached delegates or generated code.

## Remaining validation required on local machine

Run:

```bash
dotnet clean
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks
```

This environment does not include the .NET SDK, so compile validation must be done locally.
