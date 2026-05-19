# ForgeORM db.AI, db.Cte, and db.TempTable API Update

This patch moves AI querying, CTE, and temp-table creation under `db` so the public API stays consistent.

## AI query

```csharp
var answer = await db.AI.QueryAsync(
    "Top 10 customers by revenue last month",
    options => options.TenantId = tenantId,
    ct);
```

The result contains generated SQL, warnings, explanation, and dynamic dictionary rows.

## CTE from db

```csharp
var paidOrders = db.Cte<Order>(
    "PaidOrders",
    x => x.Status == OrderStatus.Paid,
    x => x.Id,
    x => x.CustomerId,
    x => x.GrandTotal);

var query = db.Select<OrderDto>()
    .WithCte(paidOrders)
    .From("PaidOrders p")
    .Columns("p.CustomerId", "SUM(p.GrandTotal) AS Revenue")
    .GroupBy("p.CustomerId");
```

## Temp table from db

```csharp
var temp = db.TempTable<Order>(
    "#TopOrders",
    x => x.Id,
    x => x.CustomerId,
    x => x.GrandTotal);
```

## Temp-table script from expression

```csharp
var script = db.TempTableFrom<Order>(
    "#PaidOrders",
    x => x.Status == OrderStatus.Paid,
    x => x.Id,
    x => x.CustomerId,
    x => x.GrandTotal);

await db.ExecuteAsync(script.Sql, script.Parameters, cancellationToken: ct);
```

## SQL-style aliases preserved under db

```csharp
var cte = db.Cte("RecentOrders", "SELECT * FROM Orders WHERE CreatedAt >= DATEADD(day, -30, GETDATE())");
var tempBuilder = db.TempTable("#ImportRows").Column("Id", "INT", false).PrimaryKey("Id");
var select = db.Select<Order>();
var scriptBuilder = db.Script();
```
