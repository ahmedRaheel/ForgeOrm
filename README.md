# ForgeORM Complete All Features

ForgeORM is a SQL-first, provider-based ORM for .NET designed to combine:

- Dapper-style raw SQL performance
- EF-like object query API
- dynamic query builder
- object mapping
- split query loading for parent/child graphs
- stored procedures
- database functions
- sync and async methods
- transactions and unit of work
- bulk insert/update/delete/merge
- query analytics
- query suggestions and autocomplete/intelligence
- SQL Server, SQL Express, PostgreSQL, MySQL, Oracle, SQLite providers
- ASP.NET Core dependency injection

## Solution Projects

```text
src/
  ForgeORM.Abstractions
  ForgeORM.Core
  ForgeORM.QueryBuilder
  ForgeORM.Mapping
  ForgeORM.Intelligence
  ForgeORM.Analytics
  ForgeORM.Providers.SqlServer
  ForgeORM.Providers.PostgreSql
  ForgeORM.Providers.MySql
  ForgeORM.Providers.Oracle
  ForgeORM.Providers.Sqlite
  ForgeORM.AspNetCore
samples/
  ForgeORM.Sample.Api
database/
  sqlserver
docs/
```

## Main API Examples

```csharp
var products = await db.QueryAsync<Product>(
    "SELECT * FROM Products WHERE Price > @Price",
    new { Price = 100 });

var product = await db.GetByIdAsync<Product>(1);

var page = await db.PageAsync<Product>(new ForgePageRequest
{
    Sql = "SELECT * FROM Products",
    OrderBy = "Id DESC",
    Page = 1,
    PageSize = 20
});

var objectQuery = await db.Set<Product>()
    .Where(x => x.Price > 100)
    .OrderByDescending(x => x.Id)
    .Skip(0)
    .Take(20)
    .ToListAsync();

await db.BulkInsertAsync(products);

await using var tx = await db.BeginTransactionAsync();
await tx.ExecuteAsync("UPDATE Products SET Price = Price + 1");
await tx.CommitAsync();
```

## Note

This is a complete architectural foundation. Bulk engines are provider hooks with safe default implementations. Production-grade provider-specific TVP/COPY/array-binding engines can be evolved inside each provider without changing Core.


## NextGen Dream APIs Added

```csharp
var rows = await db.SmartSql<Product>($"SELECT Id, Code, Name, Price FROM Products WHERE Price > {minPrice}")
    .WhereSql($"Name <> {""}")
    .AsCached(TimeSpan.FromMinutes(5))
    .WithPolicy(new ForgeResiliencePolicy { RetryCount = 2 })
    .ToShapeAsync<ProductDto>();

var json = await db.SmartSql<Product>("SELECT * FROM Products").IntoJsonAsync();
var plan = db.SmartSql<Product>("SELECT * FROM Products").Explain();
await foreach (var item in db.SmartSql<Product>("SELECT * FROM Products").StreamAllAsync()) { }
```


## Sample API Swagger

Run the sample API and open `/swagger` to test raw SQL, object query, query builder, stored procedure, NextGen schema-aware SQL, trace visualizer, semantic search, API request reflection and bulk endpoints.
