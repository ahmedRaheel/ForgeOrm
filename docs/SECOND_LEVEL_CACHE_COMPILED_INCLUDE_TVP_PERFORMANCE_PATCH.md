# ForgeORM Second-Level Cache + Compiled Include + Split Query Performance Patch

Applied in this patch:

- `db.Set<T>().Where(...).CacheFor(TimeSpan...)` opt-in second-level query result cache.
- `NoCache()` opt-out hook for query instances.
- Compiled include/graph plan cache for `Include + ThenInclude + AsSplitQuery + UseIdentityResolution` shape reuse.
- Split-query id batching helper for stable `IN @Ids` parameter shapes and SQL Server TVP integration points.
- Prepared command template pool for provider-direct SQL Server command shapes.
- Projection-reader plan cache for `.Select(x => new Dto(...))` projection-only DTO paths.
- `UseReadReplica()` remains on `IForgeExecutableQuery` and participates in cache keys/query options.
- `WarmupAsync<T>()` startup hook for metadata, include plan, and projection plan warmup.
- `StreamAsync()` now uses `QueryStreamAsync()` when possible instead of materializing a list first.

Example:

```csharp
var products = await db.Set<Product>()
    .Where(x => x.IsActive)
    .CacheFor(TimeSpan.FromMinutes(5))
    .UseReadReplica()
    .ToListAsync(ct);

await db.WarmupAsync<Order>(ct);
```

Notes:

- Cache is opt-in to avoid stale-data surprises.
- Split-query batching keeps the public EF-style API; no Dapper-style multi-mapping was added.
- SQL Server TVP helper is available for provider-specific binders to avoid large literal `IN (...)` lists.
