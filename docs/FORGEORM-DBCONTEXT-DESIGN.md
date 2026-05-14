# ForgeORM DbContext Design

ForgeORM now exposes an EF-style `ForgeDbContext` so application developers do not need to write ADO.NET, materializers, connection factories, or helper wrappers in their samples.

## Application registration

```csharp
builder.Services.AddForgeOrm(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!));
```

## Application usage

```csharp
app.MapGet("/products", async (ForgeDbContext db) =>
    await db.Set<Product>()
        .Where(x => x.Price > 100)
        .OrderByDescending(x => x.Id)
        .Take(20)
        .ToListAsync());
```

## Raw SQL still belongs to ForgeORM

```csharp
await db.QueryAsync<Product>("SELECT * FROM dbo.Products WHERE Price >= @MinPrice", new { MinPrice = 100 });
```

## Stored procedures

```csharp
await db.QueryProcedureAsync<ProductListItem>("dbo.sp_GetProductList", new { MinPrice = 100 });
```

## Bulk operations

```csharp
await db.BulkInsertAsync(products);
await db.BulkUpdateAsync(products);
await db.BulkMergeAsync(products);
```

## Transactions

```csharp
await using var tx = await db.BeginTransactionAsync();
await tx.ExecuteAsync("UPDATE dbo.Products SET Price = Price + @Amount", new { Amount = 10 });
await tx.CommitAsync();
```

The sample API should remain thin and should only call `ForgeDbContext`. All provider execution, mapping, parameters, split queries, search, bulk, transactions, telemetry, and security stay inside ForgeORM packages.
