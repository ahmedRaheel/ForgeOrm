# ForgeORM Performance Routing Fix Applied

This update addresses the benchmark plateau where ForgeORM stayed around allocation ratio `2.22` and latency regressed after provider-specific generated executors were enabled.

## Root Cause

The source-generated provider executor was being attempted for anonymous parameter objects such as:

```csharp
new { Id = id }
```

That generated path still had to parse SQL parameter names and extract parameter values dynamically, so it could be slower than the optimized framework direct executor.

## Fix

The framework now applies this order:

1. Source-generated executor for zero-parameter, scalar, or `IForgeNamedParameter` calls.
2. Framework direct executor for anonymous object parameters.
3. Compiled runtime pipeline fallback.

This preserves the framework-wide policy while avoiding the slower generated anonymous-object path.

## Additional Generator Improvements

Generated SQL Server and provider-neutral executors now fast-bind:

- `IForgeNamedParameter`
- scalar parameters

without scanning all SQL parameter names and without dynamic property extraction.

## Recommended Benchmark Call

For the lowest allocation path, prefer:

```csharp
await db.QueryFirstOrDefaultAsync<Order>(
    BenchmarkSql.QueryById,
    ForgeParameters.Id(id),
    cancellationToken: ct);
```

Anonymous objects still work and now use the safer optimized direct executor route.
