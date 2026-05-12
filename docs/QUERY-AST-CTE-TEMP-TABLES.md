# ForgeORM Query AST + String Builder Support

ForgeORM now supports both query-building options.

## Strongly typed AST API

```csharp
var query = ForgeSql
    .Select<Product>()
    .Columns(x => x.Id, x => x.Name, x => x.Price)
    .Where(x => x.Price > minPrice)
    .OrderByDescending(x => x.Id)
    .Take(20)
    .Render(db.Provider);

var rows = await db.QueryAsync<Product>(query.Sql, query.Parameters);
```

## Existing dynamic string-builder style API

```csharp
var query = builder
    .Select("Id", "Name", "Price")
    .From("Products")
    .Where("Price > @Price", new { Price = 100 })
    .Build();
```

## CTE support

```csharp
var query = ForgeSql
    .Select<Product>()
    .WithCte("LatestProducts", """
        SELECT *, ROW_NUMBER() OVER(PARTITION BY Code ORDER BY Id DESC) rn
        FROM Products
    """)
    .From("LatestProducts")
    .WhereSql("rn = 1")
    .Render(db.Provider);
```

## Temp table script support

```csharp
var script = ForgeSql.Script()
    .CreateTempTable("#ProductIds", t => t
        .Column("Id", "INT", nullable: false)
        .PrimaryKey("Id"))
    .InsertIntoTemp("#ProductIds", "SELECT Id FROM Products WHERE Price > @Price")
    .Statement("""
        SELECT p.*
        FROM Products p
        INNER JOIN #ProductIds ids ON ids.Id = p.Id
    """)
    .Render(db.Provider);
```

Provider rendering:
- SQL Server uses `#TempTable`.
- PostgreSQL renders `CREATE TEMP TABLE`.
- Oracle renders `CREATE GLOBAL TEMPORARY TABLE`.
- SQL Server/Oracle pagination uses `OFFSET/FETCH`.
- PostgreSQL/MySQL/SQLite pagination uses `LIMIT/OFFSET`.
