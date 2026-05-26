# Real Hot Path Fix Notes

This patch targets the exact benchmark paths that were still slow/allocation-heavy:

- `ForgeDb.GetById<T>(object id)`
- `ForgeDb.GetById<T,TKey>(TKey id)`
- `ForgeDb.QueryFirstOrDefault<T>(...)`

## What changed

1. `ForgeFrameworkExecutionPolicy.FirstOrDefault` now routes SQL Server text queries directly to `ForgeSqlServerProviderDirectHotPath.QueryFirstOrDefault`.
2. The sync public API no longer goes through `ValueTask.AsTask().GetAwaiter().GetResult()` for SQL Server `QueryFirstOrDefault`.
3. SQL Server detection was fixed. It now accepts provider names/types containing `SqlServer` or `SqlClient`, instead of only exact `ProviderName == "SqlServer"`.
4. `GetById` now reliably uses the direct SQL Server executor instead of accidentally falling back to the generic framework pipeline.
5. The direct SQL Server executor keeps the hot path on:
   - `SqlConnection`
   - `SqlCommand`
   - typed `SqlParameter`
   - `SqlDataReader`
   - `CommandBehavior.SingleRow | CommandBehavior.SequentialAccess`
   - cached `Func<SqlDataReader,T>` materializer

## Why v3 did not improve your shown numbers

The previous compiled-query addition was a separate API. Your benchmark uses `GetById` and `QueryFirstOrDefault`, so the measured path was still entering the generic framework/async bridge in places. This patch changes those exact methods.

## Benchmark expectation

For the fastest `GetById` benchmark use:

```csharp
_db.GetById<Order, int>(id);
```

`GetById<Order>(id)` can still box the key at the call boundary because its public signature is `object id`, but it now routes to the SQL Server direct executor.
