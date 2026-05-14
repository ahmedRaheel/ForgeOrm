# ForgeORM

High-performance next-generation .NET ORM combining the power of raw SQL, Dapper-level speed, EF Core-style developer experience, compile-time safety, dynamic query generation, schema artifacts, split-query relationship loading, and advanced database tooling.

---

# Vision

ForgeORM is designed to become:

- Faster than EF Core
- More developer friendly than Dapper
- SQL-first without losing type safety
- Database transparent
- IDE-aware
- Multi-database
- Enterprise-ready
- Cloud-native
- AI-assisted
- Compile-time optimized

ForgeORM provides:

- Raw SQL execution
- Strongly typed query AST
- Dynamic query builder
- Stored procedures
- Functions
- Bulk operations
- Split query relationship loading
- Artifact management
- Query visualization
- Query analytics
- Smart pagination
- Optional filtering
- Dynamic search APIs
- Automatic schema history
- Future Roslyn + Source Generator support

---

# Supported Databases

| Database | Support |
|---|---|
| SQL Server | ✅ |
| SQL Express | ✅ |
| PostgreSQL | ✅ |
| MySQL | ✅ |
| Oracle | ✅ |
| SQLite | ✅ |

---

# Core Philosophy

ForgeORM supports ALL development styles:

| Style | Supported |
|---|---|
| Raw SQL | ✅ |
| Dapper-like | ✅ |
| EF-like expressions | ✅ |
| Dynamic query builder | ✅ |
| AST query builder | ✅ |
| Stored procedures | ✅ |
| Functions | ✅ |
| Views | ✅ |
| Temp tables | ✅ |
| CTEs | ✅ |
| Split queries | ✅ |
| Bulk operations | ✅ |

---

# Installation

```bash
dotnet add package ForgeORM.SqlServer
dotnet add package ForgeORM.PostgreSql
dotnet add package ForgeORM.MySql
dotnet add package ForgeORM.Oracle


# Configuration

```bash
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=<DatabaseServer>;Initial Catalog=ForgeOrmDb;Integrated Security=True;TrustServerCertificate=True;"
  }
}

# Installation

```csharp

builder.Services.AddForgeOrm(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));

    options.EnableQueryLogging();
    options.EnableQueryAnalytics();
    options.EnableArtifactHistory();
});


Quick Start
```csharp
var products = await db.QueryAsync<Product>(
    "SELECT * FROM Products WHERE Price > @Price",
    new { Price = 100 });
```
AST Query Builder
```csharp
var query = ForgeSql
    .Select<Product>()
    .From("Products p")
    .LeftJoin<Category>((p, c) => p.CategoryId == c.Id)
    .Columns(
        "p.Id",
        "p.Name",
        "p.Price",
        "c.Name AS CategoryName")
    .Where(x => x.Price > 100)
    .OrderByDescending(x => x.Id)
    .Take(20)
    .Render(db.ProviderName);
```
Universal Search API
```csharp
var result = await db.Search<Product>()
    .Where(x => x.Price >= minPrice)
    .WhereIf(categoryId != null,
        x => x.CategoryId == categoryId)
    .Page(page, pageSize)
    .ToPagedAsync();
```
Bulk Operations
```csharp
await db.BulkInsertAsync("Products", rows);

await db.BulkUpdateAsync(
    "Products",
    rows,
    x => x.Id);

await db.BulkDeleteAsync<Product>(
    x => ids.Contains(x.Id));
```
Split Query Loading
```csharp
var customers = await db.SplitGraph<Customer>()
    .IncludeMany<Order, int>(
        ids => "SELECT * FROM Orders WHERE CustomerId IN @Ids",
        c => c.Id,
        o => o.CustomerId,
        (c, orders) => c.Orders = orders.ToList())
    .ToListAsync("SELECT * FROM Customers");
```
Artifact System
```csharp
var artifact = ForgeSql
    .Select<Product>()
    .From("Products")
    .AsView("vw_ProductList")
    .Render(db.ProviderName);
```
Swagger
```text
https://localhost:5001/swagger
```
Philosophy
ForgeORM is:
SQL-first
strongly typed
performance-first
transparent
enterprise-grade
future-ready
Author
Developed by Raheel Ahmed

## V2/V3 Enterprise + AI Modules Update

This package now includes additive implementations for:

- Redis/distributed query caching with memory fallback
- Telemetry and monitoring snapshots
- SQL security validation, data masking and AES column encryption helpers
- Vector search with cosine similarity and SQL Server/PostgreSQL SQL generation helpers
- AI query optimization
- AI diagnostics
- AI Minimal API code generation
- AI migration planning
- Studio API for query visualization, ERD, API testing, SaaS tenants and monitoring
- React Studio shell under `studio/ForgeORM.Studio.Web`

See `docs/V2-V3-IMPLEMENTED-MODULES.md`.


## V4 Platform Modules Added

See `docs/V4-PLATFORM-MODULES-IMPLEMENTED.md`. Added RAG, workflow engine, event sourcing, realtime, AI agents, low-code ERP generator, cloud/IaC generator, identity policy engine, offline sync, marketplace, data virtualization, time travel, AI observability and AI memory.
