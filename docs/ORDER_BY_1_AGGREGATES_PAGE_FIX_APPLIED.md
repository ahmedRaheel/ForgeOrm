# ForgeORM ORDER BY 1, Aggregates, and PageAsync Fix Applied

Applied directly inside this ForgeORM solution:

- Fixed expression paging SQL generation so `Skip`/`Take` without explicit ordering emits `ORDER BY 1` before `OFFSET/FETCH`.
- Updated SQL Server provider fallback from `ORDER BY (SELECT 1)` to `ORDER BY 1`.
- Added aggregate methods to `IForgeQuery<T>`: `SumAsync`, `AverageAsync`, `MinAsync`, `MaxAsync`.
- Added expression `PageAsync(int page, int pageSize, CancellationToken)` returning `ForgePagedResult<T>`.
- Added implementation in `ForgeORM.Core.Support.ForgeQuery<T>`.

Example generated paging SQL now becomes:

```sql
SELECT * FROM Orders WHERE CustomerId = 1
ORDER BY 1
OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY
```
