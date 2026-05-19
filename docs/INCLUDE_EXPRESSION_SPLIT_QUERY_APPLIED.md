# Include Expression Split Query Applied

This solution was updated so navigation loading is explicit through expression `Include(...)` instead of blanket `includeChildren` loading.

## Usage

Parent only:

```csharp
var orders = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .ToListAsync();
```

Parent + collection navigation:

```csharp
var orders = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .Include(x => x.Items)
    .ToListAsync();
```

Parent + reference navigation:

```csharp
var orders = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .Include(x => x.Customer)
    .FirstOrDefaultAsync();
```

Parent + both:

```csharp
var order = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .Include(x => x.Customer)
    .Include(x => x.Items)
    .FirstOrDefaultAsync();
```

## Behavior

- `Include(x => x.Items)` loads `List<TChild>` by split query.
- `Include(x => x.Customer)` loads class/reference navigation by split query.
- Parent query remains scalar-column only.
- Aggregates (`Count`, `Any`, `Sum`, `Average`, `Min`, `Max`) do not load navigations.
- Paging keeps `ORDER BY 1` fallback.
