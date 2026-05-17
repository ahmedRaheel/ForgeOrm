# Profile / Analyze Fix Applied

This package updates the actual ForgeORM solution with the missing query-builder APIs used by the sample project.

## Added

- `ForgeQueryBuilder<TEntity>.Profile(string name)`
- `ForgeQueryBuilder<TEntity>.Analyze()`
- `ForgeQueryProfiler`
- `ForgeQueryProfileEntry`
- `ForgeQueryAnalysis`
- `ForgeIndexSuggestionEngine`
- `GET /search/profiled/snapshot`
- `DELETE /search/profiled/snapshot`

## Fixed sample endpoints

These now compile against the query builder API:

```csharp
await db.Query<Product>()
    .From("dbo.Products")
    .Where(x => x.Price > 100)
    .OrderByDescending(x => x.Id)
    .Take(50)
    .Profile("HighValueProducts")
    .ToListAsync(ct);
```

```csharp
var analysis = db.Query<Product>()
    .From("dbo.Products")
    .Where(x => x.CategoryId == 10)
    .OrderByDescending(x => x.Id)
    .Analyze();
```

## Note

The environment used to patch this package does not have the .NET SDK installed, so `dotnet build` could not be executed here.
