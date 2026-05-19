# ForgeORM Query Performance Optimizations Applied

This update focuses on the benchmark bottlenecks reported for paging and expression queries.

## Applied changes

1. **Cached materialization plans**
   - Added schema-aware plan cache in `ForgeMaterializer`.
   - Avoids repeated property discovery and per-row dictionary creation.
   - Uses compiled property setters instead of `PropertyInfo.SetValue`.

2. **Scalar-only mapping**
   - Navigation properties such as `List<T>` and reference objects are no longer treated as SQL columns.
   - Prevents invalid generated columns such as `Items`.

3. **Explicit SELECT columns**
   - `ForgeQuery<T>` now generates `SELECT Id, ...` from scalar metadata instead of `SELECT *`.
   - Reduces unnecessary data transfer and avoids navigation-property column errors.

4. **Cached metadata resolver**
   - Replaced normal dictionary metadata cache with `ConcurrentDictionary`.
   - Metadata now stores scalar database columns only.

5. **Lower allocation query execution**
   - `ForgeAdo.QueryAsync<T>` now pre-sizes result lists when SQL contains `FETCH NEXT n` or `TOP n`.
   - Parameter property reflection is cached.

6. **DateTime safety**
   - Default or SQL-invalid `DateTime` values are normalized to `DateTime.UtcNow`.
   - Default `DateTimeOffset` values are normalized to `DateTimeOffset.UtcNow`.
   - SQL parameter DbType is set to `DateTime2` / `DateTimeOffset` where applicable.

## Notes

- `includeChildren` should remain `false` by default for benchmarks and hot-path queries.
- Child/reference navigation loading should only run when explicitly requested.
- For best paging results, prefer explicit ordering: `.OrderByDescending(x => x.Id)`.
