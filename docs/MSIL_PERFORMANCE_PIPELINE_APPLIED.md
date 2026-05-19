# ForgeORM MSIL Performance Pipeline Applied

This patch applies a central hot-path architecture across ForgeORM.

## Included changes

- MSIL `DbDataReader` materializer reuse through `ForgeIlMaterializerCache` and public wrapper `ForgeRuntimeReaderCompiler`.
- MSIL parameter binder through `ForgeAdo` using `DynamicMethod` and `ConcurrentDictionary<Type, Action<DbCommand, object>>`.
- Reflection-free parameter getter after first plan build.
- SQL Server parameter type assignment without reflective `PropertyInfo.SetValue`.
- Cached entity metadata in `ForgeRuntimeEntityMetadataCache`.
- MSIL entity property getters/setters through `ForgeRuntimePropertyPlan`.
- Cached query plan shapes in `ForgeRuntimeQueryPlanCache`.
- Central execution wrapper in `ForgePerformancePipeline`.
- `ForgeDb` performance APIs:
  - `PreWarm(params Type[] entityTypes)`
  - `QueryFastAsync<T>()`
  - `QueryStreamAsync<T>()`
  - `ExecuteFastAsync()`
  - `InsertCompiledAsync<T>()`
  - `UpdateCompiledAsync<T>()`
  - `DeleteCompiledAsync<T>()`
  - `GetByIdCompiledAsync<T>()`
  - `GetPerformanceCacheStats()`
- Existing `QueryAsync`, `ExecuteAsync`, stored procedure query and stored procedure execute paths now route through the performance pipeline.
- `InsertFastAsync` now uses cached runtime metadata and MSIL getters instead of expression-compiled getters.
- Temporal helpers now use cached runtime metadata instead of resolving table/key/columns through reflection per call.
- Sample API endpoints added under `/performance`.
- Benchmark project updated with MSIL accessor and MSIL reader materializer benchmarks.

## Sample endpoints

- `POST /performance/prewarm`
- `GET /performance/stats`
- `GET /performance/products/query-fast`
- `GET /performance/products/stream`
- `GET /performance/products/{id}/compiled`
- `POST /performance/products/compiled-insert`
- `PUT /performance/products/{id}/compiled-update`
- `DELETE /performance/products/{id}/compiled-delete`
- `GET /performance/orders/temporal/as-of`
- `POST /performance/products/bulk-hook`
- `POST /performance/orders/graph-hook`

## Important rule

Reflection is allowed only during first-time plan creation. Runtime calls use delegates cached in `ConcurrentDictionary`.

```text
Cold path:
  entity reflection -> MSIL delegates -> cached entity/query plan

Hot path:
  cached plan -> cached parameter binder -> DbCommand -> cached DbDataReader materializer
```

## Benchmark command

```bash
dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks
```
