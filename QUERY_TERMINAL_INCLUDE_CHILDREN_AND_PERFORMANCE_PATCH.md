# ForgeORM Terminal Include Children + Query Performance Patch

Applied to the ForgeORM core solution:

- `IForgeQuery<T>.ToListAsync(bool includeChildren = false, CancellationToken ct = default)`
- `IForgeQuery<T>.FirstOrDefaultAsync(bool includeChildren = false, CancellationToken ct = default)`
- `IForgeQuery<T>.SingleAsync(bool includeChildren = false, CancellationToken ct = default)`
- `IForgeQuery<T>.SingleOrDefaultAsync(bool includeChildren = false, CancellationToken ct = default)`
- `IForgeQuery<T>.PageAsync(int page, int pageSize, bool includeChildren = false, CancellationToken ct = default)`
- Parent queries are parent-only by default.
- Navigation split queries run only when `includeChildren: true` is passed.
- Parent SELECT generation now emits scalar columns only instead of `SELECT *`.
- `ORDER BY 1` fallback remains when paging is used without explicit ordering.
- Materialization now uses cached plans and compiled property setters.
- Parameter reflection is cached.
- Query lists are pre-sized for `TOP` / `OFFSET FETCH` result sizes.
- Default `DateTime` and `DateTimeOffset` values are normalized before SQL binding to avoid SQL Server datetime overflow.

Example:

```csharp
var parentOnly = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.Id)
    .Skip(0)
    .Take(10)
    .ToListAsync();

var withChildren = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.Id)
    .Skip(0)
    .Take(10)
    .ToListAsync(includeChildren: true);
```
