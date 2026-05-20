# Generic Key, Dapper-Style Parameters, and Split Query Fix

This patch removes the hardcoded `Id` assumption from the core metadata and provider-direct hot path.

## Key resolution

ForgeORM now resolves primary keys in this order:

1. `[ForgeKey]`
2. `[Key]`
3. `Id`
4. `<EntityName>Id` such as `OrderId`, `ProductId`, `CustomerId`
5. first property ending with `Id`

This applies to:

- `GetByIdAsync`
- generated SQL metadata
- provider-direct SQL Server hot path
- update/delete SQL generation
- graph metadata
- split-query relationship loading

## Dapper-style default behavior

Attributes are optional. These work without attributes:

```csharp
public sealed class Order
{
    public int OrderId { get; set; }
    public decimal GrandTotal { get; set; }
}
```

```csharp
await db.GetByIdAsync<Order>(orderId);
```

Generated SQL becomes:

```sql
SELECT * FROM Order WHERE OrderId = @OrderId
```

not:

```sql
WHERE Id = @Id
```

## Parameter binding

Parameter binding remains Dapper-like:

```csharp
await db.QueryAsync<Order>(
    "SELECT * FROM Orders WHERE CustomerId = @CustomerId AND Status = @Status",
    new { CustomerId = customerId, Status = "Paid" });
```

Dictionary and POCO parameter objects are supported. Scalar values are only used when the SQL has a single or scalar-compatible token.

## Split query fix

Split queries now correctly expand enumerable parameters for SQL Server provider-direct execution:

```sql
WHERE CustomerId IN @ParentIds
```

becomes:

```sql
WHERE CustomerId IN (@ParentIds0, @ParentIds1, ...)
```

This fixes one-to-one, one-to-many, and many-to-many split-query failures caused by SQL Server receiving raw `IN @Ids` syntax.

## Provider-direct GetById fix

The provider-direct SQL Server hot path now uses the actual resolved key column and parameter name:

```sql
WHERE OrderId = @OrderId
```

or:

```sql
WHERE CustomerKey = @CustomerKey
```

instead of hardcoding:

```sql
WHERE Id = @Id
```
