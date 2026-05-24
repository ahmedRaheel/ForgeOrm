# ForgeORM Performance Pipeline Normalization Patch Applied

This patch applies the requested next-stage performance architecture improvements.

## Applied

1. **Normalized public query APIs through the compiled performance pipeline**
   - `ForgeDb.QueryAsync<T>` now uses `ForgePerformancePipeline.QueryAsync<T>`.
   - `QueryFirstOrDefaultAsync<T>` and `QuerySingleOrDefaultAsync<T>` now use the pipeline single-row paths.
   - Stored procedure query/scalar async paths now use the same compiled pipeline.
   - `DbConnection` extension async query/scalar/single APIs now use the pipeline.
   - Transaction scoped support async query/scalar paths now use the pipeline.

2. **Removed `MethodInfo.Invoke` from fallback binder hot path**
   - Fallback binder property access now compiles `Func<object, object?>` getters using expression trees once per parameter type/SQL shape.
   - Execution-time binding now calls cached delegates instead of reflection invoke.

3. **Added `ExecuteScalarAsync<T>` to `ForgePerformancePipeline`**
   - Scalar execution now reuses compiled command plans and binders.
   - Enum and nullable scalar conversion are handled centrally.

4. **Routed `PageAsync` count query through the compiled pipeline**
   - Paging count no longer calls `ForgeAdo.ExecuteScalarAsync` directly.

5. **Added typed binder abstraction**
   - New `IForgeParameterBinder<T>` interface.
   - Source-generated provider interface now supports `TryGetTypedBinder<T>`.
   - Source generators emit typed binder classes alongside legacy object binders.

6. **Added provider-specific materializer hook**
   - New `IForgeProviderMaterializer` and `ForgeProviderMaterializerRegistry`.
   - `ForgeCompiledReaderResolver` checks provider-specific materializers before MSIL fallback.
   - SQL Server provider now registers a typed-reader materializer hook for future provider-direct reader specialization.

## Remaining Future Work

- Implement true SQL Server/Npgsql/MySql/Oracle direct typed materializer emit inside provider packages.
- Add command/parameter object pooling per connection with safe lifecycle controls.
- Add CI BenchmarkDotNet thresholds to prevent regression against Dapper.
