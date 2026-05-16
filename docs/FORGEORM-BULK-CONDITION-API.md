# ForgeORM Bulk Condition API

This pass adds user-friendly bulk helpers for both SQL-first and expression-first usage.

## Get by ids

```csharp
var orders = await db.GetByIdsAsync<Order, Guid>(orderIds, ct);

var customerOrders = await db.GetByIdsAsync<Order, Guid>(
    x => x.CustomerId,
    customerIds,
    ct);

var rows = await db.GetByIdsSqlAsync<Order, Guid>(
    "CustomerId",
    customerIds,
    ct);
```

## Delete by ids

```csharp
await db.DeleteByIdsAsync<Order, Guid>(orderIds, ct);

await db.DeleteByIdsAsync<Order, Guid>(
    x => x.CustomerId,
    customerIds,
    ct);

await db.DeleteByIdsSqlAsync<Order, Guid>(
    "CustomerId",
    customerIds,
    ct);
```

## Update by ids

```csharp
await db.UpdateByIdsAsync<Order, Guid>(
    orderIds,
    new { Status = OrderStatus.Cancelled, UpdatedAt = DateTimeOffset.UtcNow },
    ct);

await db.UpdateByIdsAsync<Order, Guid>(
    x => x.CustomerId,
    customerIds,
    new { Status = OrderStatus.Archived },
    ct);

await db.UpdateByIdsSqlAsync<Order, Guid>(
    "CustomerId",
    customerIds,
    new { Status = OrderStatus.Archived },
    ct);
```

## Delete by condition

```csharp
await db.DeleteByConditionAsync<Order>(
    x => x.IsDeleted && x.TotalAmount <= 0,
    ct);

await db.DeleteByConditionSqlAsync<Order>(
    "IsDeleted = 1 AND TotalAmount <= @Amount",
    new { Amount = 0 },
    ct);
```

## Update by condition

```csharp
await db.UpdateByConditionAsync<Order>(
    new { Status = OrderStatus.Cancelled, UpdatedAt = DateTimeOffset.UtcNow },
    x => x.CreatedAt < cutoffDate,
    ct);

await db.UpdateByConditionSqlAsync<Order>(
    new { Status = OrderStatus.Cancelled },
    "CreatedAt < @CutoffDate",
    new { CutoffDate = cutoffDate },
    ct);
```

## QueryAst condition rendering

```csharp
var delete = ForgeSql.Select<Order>()
    .Where(x => x.IsDeleted)
    .RenderDelete(db.Provider);

await db.ExecuteAsync(delete.Sql, delete.Parameters, cancellationToken: ct);

var update = ForgeSql.Select<Order>()
    .WhereIds(orderIds)
    .RenderUpdate(db.Provider, new { Status = OrderStatus.Cancelled });

await db.ExecuteAsync(update.Sql, update.Parameters, cancellationToken: ct);
```
