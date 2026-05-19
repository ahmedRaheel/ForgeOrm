# ForgeORM performance optimization applied

Applied to uploaded ForgeOrm(17).zip.

## Changes

1. `ForgeMaterializer` now caches per-type materialization plans with `ConcurrentDictionary<Type, MaterializerTypePlan>`.
2. Property mapping no longer scans writable properties on every row.
3. Property assignment uses compiled setter delegates instead of `PropertyInfo.SetValue` in the hot path.
4. Parameterized-constructor mapping uses a per-row ordinal lookup instead of repeatedly scanning reader columns for every constructor parameter.
5. `ReflectionForgeEntityMetadataResolver` now uses `ConcurrentDictionary` with `GetOrAdd` to avoid repeated metadata rebuilds and improve thread safety.
6. `ForgeQuery<T>` no longer emits `SELECT *` for normal entity queries. It now emits explicit non-computed mapped columns from entity metadata.
7. Existing SQL Server paging fallback remains: when `Skip`/`Take` is used without explicit ordering, generated SQL includes `ORDER BY 1` before `OFFSET/FETCH`.

## Expected benchmark impact

These changes target the allocation and CPU hot paths shown in the benchmark results:

- lower per-row materialization allocations
- lower reflection overhead
- more stable paging performance as `Take` increases
- reduced SQL payload because generated SELECT lists are explicit instead of `SELECT *`

## Build note

The container used to patch the ZIP does not have the .NET SDK installed, so `dotnet build` could not be executed here. Please run locally:

```bash
dotnet restore
dotnet build ForgeORM.sln -c Release
dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks
```
