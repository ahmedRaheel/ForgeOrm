# ForgeORM Performance Quickstart

## 1. Register ForgeORM

```csharp
builder.Services.AddForgeOrm(options => options.UseSqlServer(connectionString));
```

## 2. Query using the optimized pipeline

```csharp
var orders = await db.QueryAsync<Order>(
    "select Id, OrderNo, GrandTotal from Orders where CustomerId = @CustomerId",
    new { CustomerId = 10 },
    cancellationToken: ct);
```

## 3. Stream large results

```csharp
await foreach (var row in db.StreamAsync<Order>(sql, new { Status = "Paid" }, ct))
{
    // low allocation processing
}
```

## 4. Run benchmarks

```bash
dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks
```
