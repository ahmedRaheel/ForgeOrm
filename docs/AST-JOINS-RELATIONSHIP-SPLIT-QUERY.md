# ForgeORM AST, Joins, CTE, Temp Tables, and Split Relationships

## Strongly typed AST builder

```csharp
var query = ForgeSql
    .Select<Product>()
    .Columns(x => x.Id, x => x.Name, x => x.Price)
    .Where(x => x.Price > minPrice)
    .Render(db.Provider);
```

## String-builder option remains available

```csharp
var query = builder
    .Select("Id", "Name", "Price")
    .From("Products")
    .Where("Price > @Price", new { Price = 100 })
    .Build();
```

## Join support

```csharp
var query = ForgeSql
    .Select<Product>()
    .From("Products p")
    .InnerJoin("Categories c", "c.Id = p.CategoryId")
    .LeftJoin("Brands b", "b.Id = p.BrandId")
    .RightJoin("Suppliers s", "s.Id = p.SupplierId")
    .FullJoin("Warehouses w", "w.ProductId = p.Id")
    .CrossJoin("Regions r")
    .CrossApply("SELECT TOP 1 * FROM ProductPrices pp WHERE pp.ProductId = p.Id", "latestPrice")
    .OuterApply("SELECT TOP 1 * FROM Reviews rv WHERE rv.ProductId = p.Id", "latestReview")
    .Render(db.Provider);
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

## Temp table support

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

## Split query relationships

### One-to-one

```csharp
var customers = await db.SplitGraph<Customer>()
    .IncludeOne<CustomerProfile, int>(
        ids => "SELECT * FROM CustomerProfiles WHERE CustomerId IN @Ids",
        c => c.Id,
        p => p.CustomerId,
        (c, profile) => c.Profile = profile)
    .ToListAsync("SELECT * FROM Customers");
```

### One-to-many

```csharp
var customers = await db.SplitGraph<Customer>()
    .IncludeMany<Order, int>(
        ids => "SELECT * FROM Orders WHERE CustomerId IN @Ids",
        c => c.Id,
        o => o.CustomerId,
        (c, orders) => c.Orders = orders.ToList())
    .ToListAsync("SELECT * FROM Customers");
```

### Many-to-many

```csharp
var products = await db.SplitGraph<Product>()
    .IncludeManyToMany<ProductCategory, Category, int, int>(
        ids => "SELECT * FROM ProductCategories WHERE ProductId IN @Ids",
        ids => "SELECT * FROM Categories WHERE Id IN @Ids",
        p => p.Id,
        pc => pc.ProductId,
        pc => pc.CategoryId,
        c => c.Id,
        (p, categories) => p.Categories = categories.ToList())
    .ToListAsync("SELECT * FROM Products");
```
