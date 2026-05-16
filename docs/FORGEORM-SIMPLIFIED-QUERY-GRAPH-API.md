# ForgeORM Simplified Query, Split Graph, and Graph Insert API

This update keeps SQL and expression styles together but clearly named:

- `Where(...)`, `Join(...)`, `OrderBy(...)` for expression-first usage.
- `WhereSql(...)`, `JoinSql(...)`, `OrderBySql(...)` for SQL-first usage.
- `WhereIf(...)` and `WhereSqlIf(...)` for optional filters.
- `Any()` and `AnyAsync(...)` on regular query, split query, and QueryAst rendered SQL.

## Optional filters

```csharp
var query = db.From<Order>()
    .Where(x => !x.IsDeleted)
    .WhereIf(request.CustomerId.HasValue, x => x.CustomerId == request.CustomerId!.Value)
    .WhereSqlIf(request.Keyword is not null,
        "OrderNumber LIKE @Keyword",
        new { Keyword = $"%{request.Keyword}%" });

var exists = await query.AnyAsync(ct);
```

## QueryAst SQL + expression usage

```csharp
var ast = ForgeSql.Select<Order>()
    .From("Orders o")
    .InnerJoinSql("Customers c", "c.Id = o.CustomerId")
    .ColumnsSql("o.Id", "o.OrderNumber", "c.FullName AS CustomerName")
    .Where(x => !x.IsDeleted)
    .WhereSqlIf(request.CustomerId.HasValue, "o.CustomerId = @CustomerId", new { request.CustomerId });

var sql = ast.Render(provider);
var countSql = ast.RenderCount(provider);
var anySql = ast.RenderAny(provider);
```

## Simple one-to-many split graph for DTOs

```csharp
var orders = await db.Split<OrderDetailsDto>()
    .IncludeMany<OrderItemDto>(
        childTable: "OrderItems",
        parentKey: "Id",
        childForeignKey: "OrderId",
        target: x => x.Items,
        childWhereSql: "IsDeleted = 0")
    .ToListAsync(parentSql, new { Id = orderId }, ct);
```

## Simple one-to-many split graph for domain entities

```csharp
var orders = await db.Split<Order>()
    .IncludeMany<OrderItem>(
        childTable: "OrderItems",
        parentKey: "Id",
        childForeignKey: "OrderId",
        backingField: "_items",
        childWhereSql: "IsDeleted = 0")
    .ToListAsync(parentSql, new { Id = orderId }, ct);
```

## Simple graph insert for entities

```csharp
await db.InsertGraphAsync(
    parent: order,
    children: x => x.Items,
    parentKey: x => x.Id,
    childForeignKey: x => x.OrderId,
    cancellationToken: ct);
```

## Simple graph insert for DTOs

```csharp
await db.InsertGraphAsync<CreateOrderRequest, Order, CreateOrderItemRequest, OrderItem, Guid>(
    dto: request,
    parentFactory: x => Order.Create(x.CustomerId, x.OrderNumber),
    children: x => x.Items,
    childFactory: (order, item) => OrderItem.Create(order.Id, item.ProductId, item.ProductName, item.Quantity, item.UnitPrice),
    parentKey: x => x.Id,
    childForeignKey: x => x.OrderId,
    cancellationToken: ct);
```
