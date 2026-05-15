# ForgeORM Record Mapping, DTO Insert, and Graph Insert with SQL Server TVP

This update keeps ForgeORM as a real Dapper/EF alternative by moving the heavy work into the ForgeORM library instead of the sample application.

## 1. Record / constructor mapping

ForgeORM now supports DTO records and immutable projection records:

```csharp
public sealed record OrderSummaryRecord(
    int Id,
    string OrderNo,
    string Status,
    decimal GrandTotal,
    DateTimeOffset CreatedAt);
```

Usage:

```csharp
var rows = await db.QueryAsync<OrderSummaryRecord>(
    """
    SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
    FROM dbo.Orders
    WHERE CustomerId = @customerId
    ORDER BY CreatedAt DESC
    """,
    new { customerId });
```

ForgeORM maps constructor parameters by column name, case-insensitive. This supports C# records without requiring a parameterless constructor.

## 2. DTO-to-entity insert

Users can insert from a request DTO while telling ForgeORM the real target entity:

```csharp
await db.InsertAsync<Order, CreateOrderRequest>(request, ct);
```

ForgeORM maps matching DTO properties to the entity and runs the insert using the entity metadata.

## 3. Parent-child graph insert

ForgeORM now supports EF-style graph insert convenience:

```csharp
var id = await db.InsertGraphAsync<Order, CreateOrderRequest, int>(
    request,
    graph =>
    {
        graph.Parent().Key(x => x.Id);

        graph.Children<OrderItem, CreateOrderItemRequest>(x => x.Items)
            .ForeignKey(x => x.OrderId)
            .UseSqlServerTvp("dbo.OrderItemTvp", "dbo.InsertOrderItemsTvp");
    },
    ct);
```

ForgeORM internally:

1. Maps the parent DTO to `Order`.
2. Inserts the parent in a transaction.
3. Gets the generated parent key.
4. Maps child DTOs to `OrderItem`.
5. Assigns `OrderId` to every child row.
6. Sends child rows as a SQL Server TVP.
7. Commits or rolls back the transaction.

## 4. SQL Server TVP support

SQL type:

```sql
CREATE TYPE dbo.OrderItemTvp AS TABLE
(
    Id INT NULL,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL
);
```

Procedure:

```sql
CREATE OR ALTER PROCEDURE dbo.InsertOrderItemsTvp
    @Items dbo.OrderItemTvp READONLY
AS
BEGIN
    INSERT INTO dbo.OrderItems(OrderId, ProductId, Quantity, UnitPrice, LineTotal)
    SELECT OrderId, ProductId, Quantity, UnitPrice, LineTotal
    FROM @Items;
END
```

The sample API stays simple. Users do not write ADO.NET, data readers, mapping code, or TVP parameter plumbing.


## Enum Mapping Support

ForgeORM now supports enum conversion automatically during fetch and insert.

Default behavior stores enums as readable strings, which works well with `NVARCHAR` columns:

```csharp
public enum OrderStatus
{
    Draft,
    Pending,
    Paid,
    Shipped,
    Completed,
    Cancelled
}

public sealed record OrderSummaryRecord(
    int Id,
    string OrderNo,
    OrderStatus Status,
    decimal GrandTotal,
    DateTimeOffset CreatedAt);
```

Querying records converts database values such as `Draft`, `Paid`, or `Completed` into `OrderStatus` automatically:

```csharp
var rows = await db.QueryAsync<OrderSummaryRecord>(
    """
    SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
    FROM dbo.Orders
    WHERE Status = @status
    """,
    new { status = OrderStatus.Paid });
```

Insert/graph insert converts enum parameters back to database values automatically:

```csharp
await db.InsertAsync<Order, CreateOrderRequest>(new CreateOrderRequest
{
    CustomerId = 1,
    OrderNo = "ORD-1001",
    Status = OrderStatus.Paid
});
```

For numeric enum storage, annotate the property:

```csharp
[ForgeEnumStorage(ForgeEnumStorage.Number)]
public OrderStatus Status { get; set; }
```
