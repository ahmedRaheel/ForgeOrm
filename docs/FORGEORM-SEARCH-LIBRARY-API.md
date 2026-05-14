# ForgeORM Search API Belongs to the Library

This update moves universal search infrastructure into `ForgeORM.Core.Search` so sample projects stay thin.

Application code should call high-level APIs only:

```csharp
return await db.Search<Product>()
    .Select(x => x.Id, x => x.Code, x => x.Name, x => x.Price)
    .From("dbo.Products")
    .Optional(x => x.Code, code)
    .OptionalLike(x => x.Name, name)
    .OptionalBetween(x => x.Price, minPrice, maxPrice)
    .OrderByDescending(x => x.Id)
    .Page(page, pageSize)
    .ToPagedAsync(ct);
```

The library owns:

- SQL rendering
- optional filters
- paging
- parameter generation
- expression translation
- enum parameter normalization
- column/table metadata resolution
- stored procedure search wrappers

The sample should not contain `ForgeSearch`, `ForgeSearchExpression`, paging infrastructure, or reflection/parameter-building internals.

## Escape Hatches

ForgeORM still supports SQL-first users:

```csharp
await db.Search<Product>()
    .FromSql("SELECT Id, Code, Name, Price FROM dbo.Products")
    .WhereIf(name is not null, "Name LIKE @Name", new { Name = $"%{name}%" })
    .OrderBy("Id DESC")
    .Page(page, pageSize)
    .ToPagedAsync(ct);
```

This keeps ForgeORM flexible without pushing infrastructure work to consumers.
