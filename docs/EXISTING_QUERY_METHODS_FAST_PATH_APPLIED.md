# Existing Query Methods Fast Path Applied

This update improves the existing raw SQL query API instead of requiring benchmark-only `QueryFast*` methods.

Updated methods:

- `QueryFirstOrDefault<T>()`
- `QueryFirstOrDefaultAsync<T>()`
- `QuerySingleOrDefault<T>()`
- `QuerySingleOrDefaultAsync<T>()`
- `QueryFirst<T>()`
- `QueryFirstAsync<T>()`
- `QuerySingle<T>()`
- `QuerySingleAsync<T>()`

Behavior:

- `QueryFirstOrDefault*` now executes with `CommandBehavior.SingleRow` and maps only the first row.
- `QuerySingleOrDefault*` reads at most two rows and throws if more than one row exists.
- Existing API remains unchanged.
- Benchmark code should use existing methods such as `QuerySingleOrDefaultAsync<T>()`, not `QueryFirstFastAsync<T>()`.

Example:

```csharp
return await db.QuerySingleOrDefaultAsync<OrderDto>(
    BenchmarkSql.QueryById,
    new { Id = id },
    cancellationToken: ct);
```
