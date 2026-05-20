# ForgeORM Full Performance Hardening 1–12 Applied

This patch applies the full performance direction requested for the latest ForgeORM package.

## Applied

1. Provider-direct SQL Server hot path now uses `SqlConnection`, `SqlCommand`, `SqlDataReader`, and `SqlParameter` for high-frequency calls.
2. `GetByIdAsync` now uses a per-entity static executor plan instead of a per-call dictionary plan lookup.
3. `GetByIdAsync` emits `SELECT TOP (1)` with explicit projected columns and no list materialization.
4. Enumerable SQL parameters such as `IN @Ids` and `IN (@Ids)` are expanded provider-safely for SQL Server instead of passing `object[]` as a scalar value.
5. Enum SQL parameter binding uses string names by default so `nvarchar` enum columns such as `Paid` do not compare against numeric enum values.
6. Existing EF-style split query remains the model; no Dapper splitOn/multi-mapping API was added.
7. Split-query batching path is ready for TVP strategy while expansion fallback is used by default for correctness without requiring a pre-created SQL type.
8. Concrete provider materialization path remains `SqlDataReader`-based.
9. Query/list/first/single/stream already route through provider-direct SQL Server paths where possible.
10. Prepared command template metadata remains cached and now works after enumerable expansion, so the compiled command shape matches the actual parameter list.
11. Benchmark gate manifest now includes provider-direct, source-generated, runtime-emit, projection, split-query, and include graph benchmarks.
12. The patch keeps source-generated first / runtime-emit fallback architecture and does not add public `Fast*` APIs.

## Important behavior

`IN @Ids` now becomes:

```sql
IN (@Ids_0, @Ids_1, @Ids_2)
```

and the parameter bag becomes:

```csharp
Ids_0 = 1
Ids_1 = 2
Ids_2 = 3
```

This fixes SQL Server errors like:

```text
Failed to convert parameter value from Object[] to String
```

## Next optional TVP mode

For very large split-query batches, add a SQL Server table type:

```sql
CREATE TYPE dbo.ForgeIntIdList AS TABLE (Id INT NOT NULL);
```

Then the SQL Server provider can switch the same split-query batching contract from expansion to structured parameters.
