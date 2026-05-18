# ForgeORM Fast Lane Find Applied

Added fast-lane APIs for Dapper-level primary-key reads:

```csharp
await db.FindAsync<Order>(id);
db.Find<Order>(id);
await db.QueryFirstFastAsync<Order>(sql, new { Id = id });
await db.QueryFastAsync<Order>(sql, parameters);
```

## Why

`db.Set<T>().Where(...).FirstOrDefaultAsync()` is the expressive ORM path. It includes expression translation, query-state rendering, safety checks, include support, aggregate/page support, and graph-aware rules.

`FindAsync<T>(id)` is the performance path. It bypasses:

- expression parsing
- `Where` translation
- include navigation processing
- graph logic
- query-state rendering
- repeated SQL generation

It uses cached SQL plans per entity type and a direct single-row reader execution.

## Benchmark guidance

Compare Dapper primary-key lookup against:

```csharp
await db.FindAsync<Order>(id);
```

not against the full expression pipeline.
