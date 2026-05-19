# Search Optional Expression Fix Applied

Fixed compile error:

```csharp
Cannot convert lambda expression to type 'string' because it is not a delegate type
```

for:

```csharp
db.Search<Order>()
    .Optional(x => x.CustomerId, customerId)
    .OptionalLike(x => x.OrderNo, orderNo)
    .OptionalBetween(x => x.CreatedAt, from, to)
    .OrderByDescending(x => x.CreatedAt)
```

Cause:
- The compiler was falling back to the string overload because the expression overloads were too generic/nullable-sensitive.

Fix:
- Added concrete instance overloads for common optional expression types:
  - int / int?
  - long / long?
  - Guid / Guid?
  - decimal / decimal?
  - DateTime / DateTime?
  - DateTimeOffset / DateTimeOffset?
- Ensured sample endpoint uses `ForgeORM.Querying.Search`.
