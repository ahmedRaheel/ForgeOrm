# EF-style Split Query Include Graph Loader Applied

This patch moves ForgeORM away from Dapper-style `splitOn` multi-mapping for parent/child graphs and toward EF-style graph loading.

## Added API surface

```csharp
var orders = await db.Set<Order>()
    .Include(x => x.Items)
    .AsSplitQuery()
    .UseIdentityResolution()
    .ToListAsync(ct);
```

Also added compile-surface hooks:

```csharp
.AsSingleQuery()
.ThenInclude<TRoot, TPrevious, TProperty>(...)
```

`AsSplitQuery()` is the safe/default behavior for collections to avoid parent row explosion.

## Loader improvements

- Convention-first table/key discovery.
- Attributes remain optional.
- Key discovery supports `[ForgeKey]`, `[Key]`, `Id`, `<EntityName>Id`, and ordered composite keys.
- One-to-one/reference loading by FK convention.
- One-to-many collection loading by parent key/FK convention.
- Many-to-many join-table loading by convention.
- SQL Server-friendly `IN @Ids` parameter batching instead of string literal expansion.
- Existing `SplitGraph<T>()` still works for explicit advanced cases.

## Examples

```csharp
var customers = await db.Set<Customer>()
    .Include(x => x.Orders)
    .AsSplitQuery()
    .ToListAsync(ct);
```

```csharp
var products = await db.Set<Product>()
    .Include(x => x.Categories)
    .AsSplitQuery()
    .UseIdentityResolution()
    .ToListAsync(ct);
```

The explicit split graph API remains available when relationship/table names do not follow conventions.
