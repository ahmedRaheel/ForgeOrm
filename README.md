# ForgeORM Complete Solution Final

ForgeORM is a SQL-first, provider-based .NET ORM designed to combine Dapper-style speed, EF-like productivity, dynamic query building, strongly typed AST query building, relationship split queries, bulk operations, stored procedures/functions, transactions, SQL intelligence, and database artifact lifecycle management.

## Included Features

- Dapper-style sync/async raw SQL methods
- Stored procedure execution/query methods
- Function/scalar query methods
- `QueryMultiple` / grid reader
- EF-like object query API
- Dynamic string-builder query builder
- Strongly typed AST builder:
  - `ForgeSql.Select<T>()`
  - `.Columns(x => x.Id, ...)`
  - `.Where(x => x.Price > minPrice)`
  - `.Render(db.Provider)`
- All major joins:
  - inner
  - left
  - right
  - full
  - cross
  - cross apply
  - outer apply
- CTE support
- Temp-table script support
- Split-query relationship loading:
  - one-to-one
  - one-to-many
  - many-to-many
- Bulk operations:
  - insert
  - update
  - delete
  - merge/upsert hook
- Transactions / unit of work style API
- Query analyzer
- SQL intelligence/autocomplete contracts
- Object mapping project
- Providers:
  - SQL Server / SQL Express
  - PostgreSQL
  - MySQL
  - Oracle
  - SQLite
- ASP.NET Core DI package
- Sample API with Swagger support
- Database artifact lifecycle:
  - create/update views from AST
  - create/update stored procedures from AST
  - automatic `ForgeOrmArtifactHistory` table
  - hash comparison
  - skip unchanged artifacts
  - version history records

## Example: Strongly Typed Query

```csharp
var query = ForgeSql
    .Select<Product>()
    .Columns(x => x.Id, x => x.Name, x => x.Price)
    .Where(x => x.Price > minPrice)
    .Render(db.Provider);

var rows = await db.QueryAsync<Product>(query.Sql, query.Parameters);
```

## Example: Create View from AST

```csharp
var query = ForgeSql
    .Select<Product>()
    .Columns(x => x.Id, x => x.Name, x => x.Price)
    .Where(x => x.Price > minPrice);

var view = query
    .AsView("vw_ExpensiveProducts", "dbo")
    .WithReason("Create view from ForgeSql AST")
    .Render(db.Provider);

await artifactManager.CreateOrUpdateAsync(view.Artifact);
```

## Example: Create Stored Procedure from AST

```csharp
var proc = query
    .AsProcedure("sp_GetExpensiveProducts", "dbo")
    .WithParameter("@MinPrice", "DECIMAL(18,2)")
    .WithReason("Create procedure from ForgeSql AST")
    .Render(db.Provider);

await artifactManager.CreateOrUpdateAsync(proc.Artifact);
```
