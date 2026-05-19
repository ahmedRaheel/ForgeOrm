# Search Moved to Core

Removed the separate `ForgeORM.Querying` project dependency.

Reason:
- The sample project was referencing `ForgeORM.Querying.dll`.
- If that project was not restored/built correctly, Visual Studio produced:
  `Metadata file ... ForgeORM.Querying.dll could not be found`.

Decision:
- `Search` is core package functionality.
- Moved `ForgeSearch<T>`, `ForgeProcedureSearch<T>`, and `ForgeSearchExtensions` into:
  `src/ForgeORM.Core/Search/ForgeSearch.cs`
- Namespace is now:
  `ForgeORM.Core.Search`
- Removed sample project reference to `ForgeORM.Querying`.
- Updated sample using statements to:
  `using ForgeORM.Core.Search;`

Use:
```csharp
using ForgeORM.Core.Search;

var result = await db.Search<Order>()
    .From("dbo.Orders")
    .Optional(x => x.CustomerId, customerId)
    .OptionalLike(x => x.OrderNo, orderNo)
    .OptionalBetween(x => x.CreatedAt, from, to)
    .OrderByDescending(x => x.CreatedAt)
    .Page(page, pageSize)
    .ToPagedAsync(ct);
```
