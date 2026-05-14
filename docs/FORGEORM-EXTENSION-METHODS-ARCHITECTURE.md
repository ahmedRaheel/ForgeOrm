# ForgeORM Extension Methods Architecture

ForgeORM is positioned as an alternative to Dapper and EF Core, so consumers must not copy ADO.NET executor, mapper, grid-reader, parameter binder, or materializer code into their own applications.

The runtime now owns the low-level database pipeline:

- `ForgeAdo` internal executor/materializer
- `ForgeDb` high-level ORM facade implementing `IForgeDb`
- `ForgeDbConnectionExtensions` Dapper-like extension methods on `DbConnection`
- `ForgeGridReader` multi-result reader
- `ForgeRelationshipSplitQuery` graph/split query engine
- `ForgeSearch` universal optional search API
- `ForgeArtifactManager` view/procedure artifact lifecycle

Application/sample code should only use:

```csharp
builder.Services.AddForgeOrm(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!));
```

Then inject:

```csharp
app.MapGet("/products", async (IForgeDb db) =>
    await db.QueryAsync<Product>("SELECT * FROM dbo.Products"));
```

Advanced users can also use ForgeORM-owned DbConnection extension methods:

```csharp
await using var connection = provider.CreateConnection(connectionString);
var rows = await connection.QueryAsync<Product>(
    "SELECT * FROM dbo.Products WHERE Price >= @MinPrice",
    new { MinPrice = 100 });
```

There is no Dapper dependency and no EF Core dependency in this execution path.
