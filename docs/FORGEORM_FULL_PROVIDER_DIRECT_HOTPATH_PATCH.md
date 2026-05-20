# ForgeORM Full Provider-Direct Hot Path Patch

This patch applies the requested performance cleanup across the normal public API surface rather than adding separate `Fast*` methods.

## Applied

- `GetByIdAsync` no longer calls the generic query/list pipeline for SQL Server.
- SQL Server hot paths use concrete `SqlConnection`, `SqlCommand`, `SqlDataReader`, and `SqlParameter`.
- `GetByIdAsync` uses a per-entity direct executor: `ForgeSqlServerDirectGetByIdExecutor<T>`.
- The direct executor caches SQL, key parameter name, key type, and the row materializer.
- The direct materializer accepts `SqlDataReader` directly instead of `DbDataReader`.
- No `List<T>` allocation for single-row reads.
- No SQL generation per call for `GetById` after the first plan build.
- No generic reader resolver lookup inside the `GetById` row loop.
- Query, first/single, stream, execute, and scalar SQL Server paths are routed to provider-direct methods where possible.
- `QueryStreamAsync` now uses the SQL Server provider-direct streaming path.
- Typed `GetById<T,TKey>` / `GetByIdAsync<T,TKey>` overloads were added for callers that want to avoid boxing key values at the public API.
- Scalar conversion is centralized in `ForgeScalarConverter`.

## Remaining benchmark expectation

For SQL Server `GetByIdAsync`, the call path is now:

```text
ForgeDb.GetByIdAsync<T>(id)
  -> ForgeSqlServerProviderDirectHotPath.GetByIdAsync<T>
  -> ForgeSqlServerDirectGetByIdExecutor<T>.ExecuteAsync
  -> SqlConnection / SqlCommand / SqlDataReader
  -> cached SqlDataReader materializer
```

It should no longer go through:

```text
QueryAsync<T>
List<T>
DbCommand
DbDataReader generic resolver
runtime SQL builder per call
```

Run BenchmarkDotNet again and compare:

```text
Dapper_Query_By_Id
EF_Core_Query_By_Id
ForgeORM_Query_By_Id
```
