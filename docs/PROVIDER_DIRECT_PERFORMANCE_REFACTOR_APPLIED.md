# Provider Direct Performance Refactor Applied

This patch adds a SQL Server provider-direct hot path for the operations that dominated the benchmark allocation profile.

## Applied

- `GetById<T>()` and `GetByIdAsync<T>()` now use `SqlConnection`, `SqlCommand`, `SqlDataReader`, and typed `SqlParameter` directly for SQL Server.
- Single-row methods no longer route through list materialization.
- Query first/default direct SQL Server path added.
- Query list direct SQL Server path added while keeping `IReadOnlyList<T>` materialization.
- Streaming provider-direct helper added for `IAsyncEnumerable<T>` use.
- Prepared command metadata cache added for SQL text and parameter names.
- Per-entity GetById plan cache added.
- SQL Server parameter binder cache added for POCO/anonymous parameter bags using generated delegates.
- Enum parameters bind as string by default, matching nvarchar enum columns such as `Paid`.
- Benchmark duplicate `BenchProduct` type conflict cleaned up.

## Expected benchmark impact

The previous benchmark showed ForgeORM `GetById` around `419 us / 26 KB`, which means it was still going through the generic ADO pipeline. The new SQL Server direct path is designed to reduce:

- generic `DbCommand`/`DbDataReader` overhead
- single-row list allocation
- repeated command plan parsing
- generic parameter binder overhead
- enum SQL conversion issues

Run:

```bash
dotnet clean
dotnet restore
dotnet build -c Release
dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks
```
