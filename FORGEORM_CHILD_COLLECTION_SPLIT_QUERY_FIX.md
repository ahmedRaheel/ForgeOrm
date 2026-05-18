# ForgeORM child collection split query fix

Applied fixes:

1. Database scalar metadata now excludes navigation/collection properties such as `List<OrderItem> Items`.
   This prevents invalid SQL like `SELECT ..., Items FROM Orders`.

2. Generated expression SQL now uses explicit scalar columns instead of `SELECT *` for `db.Set<T>()`.

3. `FirstOrDefaultAsync()` now uses the query pipeline and loads child collections after the parent row is read.

4. If an entity contains a collection property such as:

```csharp
public List<OrderItem> Items { get; set; } = [];
```

ForgeORM treats it as a child collection and internally runs a split query by convention:

```sql
SELECT Id, CustomerId, OrderNo, Status, GrandTotal, CreatedAt, OrderDate, TotalAmount
FROM Orders
WHERE CustomerId = 1
ORDER BY 1
OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;

SELECT Id, OrderId, ProductId, Quantity, UnitPrice, LineTotal
FROM OrderItems
WHERE OrderId IN @Ids;
```

5. Child FK convention supported by this patch:

- Parent type `Order` + parent key `Id` => child FK `OrderId`
- `[ForgeColumn]` is respected when present.

Example:

```csharp
var order = await db.Set<Order>()
    .Where(x => x.CustomerId == customerId)
    .FirstOrDefaultAsync();

// order.Items is populated automatically when OrderItem has OrderId.
```
