# GetById + EF-Style Include/Split Query Fix

Applied in this patch:

- Restored `GetByIdAsync` to a lean SQL Server provider-direct path.
- Removed per-call `ConcurrentDictionary<string,...>` lookup from the generic `GetById` executor and replaced it with a static per-generic-type plan cache.
- Changed provider-direct `GetById` SQL from `SELECT *` to an explicit generated column list and `SELECT TOP (1)`.
- Added `CommandBehavior.SingleResult` for single-row reads.
- Kept generic primary key support: `[ForgeKey]`, `[Key]`, `Id`, and `<EntityName>Id`.
- Added real EF-style `ThenInclude` compile surface through `IForgeIncludableQuery<TRoot,TProperty>`.
- Fixed SQL Server split-query collection parameter handling: arrays/lists are expanded into scalar SQL parameters for `IN @Ids` / `IN (@Ids)` instead of being sent as a single `object[]` SqlParameter.
- Kept the existing public API; no Dapper-style split mapping was introduced.

Example now supported:

```csharp
var orders = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .Include(x => x.Customer)
    .Include(x => x.Items)
    .ThenInclude(x => x.Product)
    .AsSplitQuery()
    .ToListAsync(ct);
```
