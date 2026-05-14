
using ForgeORM.Abstractions;
using ForgeORM.AI.Advanced;
using ForgeORM.AspNetCore;
using ForgeORM.Caching.Redis;
using ForgeORM.Core;
using ForgeORM.Core.Search;
using ForgeORM.QueryAst;
using ForgeORM.QueryAst.Artifacts;
using ForgeORM.SchemaOps;
using ForgeORM.Security;
using ForgeORM.Telemetry;
using ForgeORM.VectorSearch;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddForgeOrm(options => options.UseSqlServer(connectionString));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddForgeMemoryQueryCaching();
builder.Services.AddForgeTelemetry();
builder.Services.AddForgeSecurity();
builder.Services.AddForgeInMemoryVectorSearch();
builder.Services.AddForgeAdvancedAi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "ForgeORM Sample Scenarios API");

app.MapGet("/raw/products", async (ForgeDbContext db) =>
    await db.QueryAsync<Product>("SELECT * FROM dbo.Products"))
    .WithTags("01 Raw SQL");

app.MapGet("/raw/products/{id:int}", async (int id, ForgeDbContext db) =>
    await db.QuerySingleOrDefaultAsync<Product>("SELECT * FROM dbo.Products WHERE Id = @Id", new { Id = id }))
    .WithTags("01 Raw SQL");

app.MapGet("/stored-procedure/products", async (decimal minPrice, ForgeDbContext db) =>
    await db.QueryProcedureAsync<ProductListItem>("dbo.sp_GetProductList", new { MinPrice = minPrice }))
    .WithTags("02 Stored Procedures");

app.MapGet("/function/product-count", async (ForgeDbContext db) =>
    await db.ExecuteScalarAsync<int>("SELECT dbo.fn_ProductCount()"))
    .WithTags("03 Functions");

app.MapGet("/query-multiple/dashboard", async (ForgeDbContext db) =>
{
    using var grid = await db.QueryMultipleAsync("""
        SELECT COUNT(1) TotalProducts FROM dbo.Products;
        SELECT COUNT(1) TotalCustomers FROM dbo.Customers;
        SELECT TOP 5 * FROM dbo.Products ORDER BY Id DESC;
    """);

    return Results.Ok(new
    {
        ProductCount = await grid.ReadAsync<dynamic>(),
        CustomerCount = await grid.ReadAsync<dynamic>(),
        LatestProducts = await grid.ReadAsync<Product>()
    });
})
.WithTags("04 QueryMultiple");

app.MapGet("/builder/string/products", async (decimal minPrice, IForgeDynamicQueryBuilder qb, ForgeDbContext db) =>
{
    var q = qb.Select("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName", "b.Name AS BrandName")
        .From("dbo.Products p")
        .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .LeftJoin("dbo.Brands b", "b.Id = p.BrandId")
        .Where("p.Price > @MinPrice", new { MinPrice = minPrice })
        .OrderBy("p.Id DESC")
        .Take(20)
        .Build(db.Provider);

    return await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters);
})
.WithTags("05 String Builder");

app.MapGet("/builder/ast/products", async (decimal minPrice, ForgeDbContext db) =>
{
    var q = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .LeftJoin<Category>((p, c) => p.CategoryId == c.Id)
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
        .WhereSql("p.Price > @MinPrice", new { MinPrice = minPrice })
        .OrderBySql("p.Id DESC")
        .Take(20)
        .Render(db.Provider);

    return await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters);
})
.WithTags("06 AST Builder");

app.MapGet("/builder/ast/all-joins", async (ForgeDbContext db) =>
{
    var q = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .InnerJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .LeftJoin("dbo.Brands b", "b.Id = p.BrandId")
        .CrossApply("SELECT TOP 1 o.Id FROM dbo.Orders o ORDER BY o.Id DESC", "latestOrder")
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName", "b.Name AS BrandName")
        .OrderBySql("p.Id DESC")
        .Take(20)
        .Render(db.Provider);

    return await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters);
})
.WithTags("06 AST Builder");

app.MapGet("/cte/latest-products", async (ForgeDbContext db) =>
{
    var q = ForgeSql.Select<Product>()
        .WithCte("LatestProducts", """
            SELECT *, ROW_NUMBER() OVER(PARTITION BY Code ORDER BY Id DESC) rn
            FROM dbo.Products
        """)
        .From("LatestProducts")
        .Columns("Id", "Code", "Name", "Price")
        .WhereSql("rn = 1")
        .Render(db.Provider);

    return await db.QueryAsync<Product>(q.Sql, q.Parameters);
})
.WithTags("07 CTE");

app.MapGet("/temp-table/script", (ForgeDbContext db) =>
{
    var script = ForgeSql.Script()
        .CreateTempTable("#ProductIds", t => t.Column("Id", "INT", false).PrimaryKey("Id"))
        .InsertIntoTemp("#ProductIds", "SELECT Id FROM dbo.Products WHERE Price > @MinPrice")
        .Statement("""
            SELECT p.*
            FROM dbo.Products p
            INNER JOIN #ProductIds ids ON ids.Id = p.Id
        """)
        .Render(db.Provider);

    return Results.Ok(script.Sql);
})
.WithTags("08 Temp Tables");

app.MapGet("/pagination/products", async (int page, int pageSize, ForgeDbContext db) =>
{
    var q = ForgeSql.Select<Product>()
        .From("dbo.Products")
        .Columns(x => x.Id, x => x.Code, x => x.Name, x => x.Price)
        .OrderByDescending(x => x.Id)
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Render(db.Provider);

    return await db.QueryAsync<Product>(q.Sql, q.Parameters);
})
.WithTags("09 Pagination");

app.MapPost("/bulk/products", async (List<ProductCreateRequest> rows, ForgeDbContext db) =>
{
    await db.BulkInsertAsync("dbo.Products", rows);
    return Results.Ok(new { Inserted = rows.Count });
})
.WithTags("10 Bulk");

app.MapPost("/transaction/increase-prices", async (decimal amount, ForgeDbContext db) =>
{
    await using var tx = await db.BeginTransactionAsync();

    try
    {
        await tx.ExecuteAsync("UPDATE dbo.Products SET Price = Price + @Amount", new { Amount = amount });
        await tx.CommitAsync();
        return Results.Ok(new { Updated = true });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
})
.WithTags("11 Transactions");

app.MapGet("/split/one-to-one", async (ForgeDbContext db) =>
{
    var rows = await db.SplitGraph<Customer>()
        .IncludeOne<CustomerProfile, int>(
            ids => "SELECT * FROM dbo.CustomerProfiles WHERE CustomerId IN @Ids",
            c => c.Id,
            p => p.CustomerId,
            (c, profile) => c.Profile = profile)
        .ToListAsync("SELECT * FROM dbo.Customers");

    return Results.Ok(rows);
})
.WithTags("12 Split Query");

app.MapGet("/split/one-to-many", async (ForgeDbContext db) =>
{
    var rows = await db.SplitGraph<Customer>()
        .IncludeMany<Order, int>(
            ids => "SELECT * FROM dbo.Orders WHERE CustomerId IN @Ids",
            c => c.Id,
            o => o.CustomerId,
            (c, orders) => c.Orders = orders.ToList())
        .ToListAsync("SELECT * FROM dbo.Customers");

    return Results.Ok(rows);
})
.WithTags("12 Split Query");

app.MapGet("/split/many-to-many", async (ForgeDbContext db) =>
{
    var rows = await db.SplitGraph<Product>()
        .IncludeManyToMany<ProductCategory, Category, int, int>(
            ids => "SELECT * FROM dbo.ProductCategories WHERE ProductId IN @Ids",
            ids => "SELECT * FROM dbo.Categories WHERE Id IN @Ids",
            p => p.Id,
            pc => pc.ProductId,
            pc => pc.CategoryId,
            c => c.Id,
            (p, cats) => p.Categories = cats.ToList())
        .ToListAsync("SELECT * FROM dbo.Products");

    return Results.Ok(rows);
})
.WithTags("12 Split Query");

app.MapPost("/artifacts/view/product-list", async (ForgeDbContext db, IForgeArtifactManager artifacts) =>
{
    var query = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName");

    var artifact = query.AsView("vw_ProductList", "dbo")
        .WithReason("Create view from AST")
        .Render(db.Provider);

    return Results.Ok(await artifacts.CreateOrUpdateAsync(artifact.Artifact));
})
.WithTags("13 Artifacts");

app.MapPost("/artifacts/procedure/product-list", async (ForgeDbContext db, IForgeArtifactManager artifacts) =>
{
    var query = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
        .WhereSql("p.Price >= @MinPrice");

    var artifact = query.AsProcedure("sp_ProductList_FromAst", "dbo")
        .WithParameter("@MinPrice", "DECIMAL(18,2)")
        .WithReason("Create stored procedure from AST")
        .Render(db.Provider);

    return Results.Ok(await artifacts.CreateOrUpdateAsync(artifact.Artifact));
})
.WithTags("13 Artifacts");


app.MapGet("/search/products/text", async (
    string? code,
    string? name,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDbContext db) =>
{
    // Advanced raw-SQL escape hatch stays available, but search infrastructure is inside ForgeORM.Core.
    return await db.Search<Product>()
        .FromSql("SELECT Id, Code, Name, Price FROM dbo.Products")
        .WhereIf(!string.IsNullOrWhiteSpace(code), "Code = @Code", new { Code = code })
        .WhereIf(!string.IsNullOrWhiteSpace(name), "Name LIKE @Name", new { Name = $"%{name}%" })
        .WhereIf(minPrice.HasValue, "Price >= @MinPrice", new { MinPrice = minPrice })
        .WhereIf(maxPrice.HasValue, "Price <= @MaxPrice", new { MaxPrice = maxPrice })
        .OrderByDescending(x => x.Id)
        .Page(page, pageSize)
        .ToPagedAsync();
})
.WithTags("14 Universal Search");

app.MapGet("/search/products/expression", async (
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDbContext db) =>
{
    return await db.Search<Product>()
        .From("dbo.Products")
        .WhereIf(minPrice.HasValue, x => x.Price >= minPrice!.Value)
        .WhereIf(maxPrice.HasValue, x => x.Price <= maxPrice!.Value)
        .OrderByDescending(x => x.Id)
        .Page(page, pageSize)
        .ToPagedAsync();
})
.WithTags("14 Universal Search");

app.MapGet("/search/products/builder", async (
    string? code,
    string? name,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDbContext db) =>
{
    // This is the recommended user-facing API: expression-based, IntelliSense-friendly, no internal classes exposed.
    return await db.Search<Product>()
        .Select(x => x.Id, x => x.Code, x => x.Name, x => x.Price)
        .From("dbo.Products")
        .Optional(x => x.Code, code)
        .OptionalLike(x => x.Name, name)
        .OptionalBetween(x => x.Price, minPrice, maxPrice)
        .OrderByDescending(x => x.Id)
        .Page(page, pageSize)
        .ToPagedAsync();
})
.WithTags("14 Universal Search");

app.MapGet("/search/products/procedure", async (
    string? code,
    string? name,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDbContext db) =>
{
    return await db.SearchProcedure<Product>("dbo.SearchProducts")
        .WithOptional("Code", code)
        .WithOptional("Name", name)
        .WithOptional("MinPrice", minPrice)
        .WithOptional("MaxPrice", maxPrice)
        .Page(page, pageSize)
        .ToListAsync();
})
.WithTags("14 Universal Search");



app.MapGet("/orders/customer/{customerId:int}/summary-records", async (int customerId, ForgeDbContext db, CancellationToken ct) =>
    Results.Ok(await db.QueryAsync<OrderSummaryRecord>(
        """
        SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
        FROM dbo.Orders
        WHERE CustomerId = @customerId
        ORDER BY CreatedAt DESC
        """,
        new { customerId },
        cancellationToken: ct)))
.WithTags("15 Record Mapping");

app.MapPost("/orders/insert-dto", async (CreateOrderRequest request, ForgeDbContext db, CancellationToken ct) =>
{
    var inserted = await db.InsertAsync<Order, CreateOrderRequest>(request, ct);
    return Results.Ok(new { Inserted = inserted });
})
.WithTags("16 DTO Insert");

app.MapPost("/orders/insert-graph-tvp", async (CreateOrderRequest request, ForgeDbContext db, CancellationToken ct) =>
{
    request.GrandTotal = request.Items.Sum(x => x.Quantity * x.UnitPrice);

    var id = await db.InsertGraphAsync<Order, CreateOrderRequest, int>(
        request,
        graph =>
        {
            graph.Parent().Key(x => x.Id);
            graph.Children<OrderItem, CreateOrderItemRequest>(x => x.Items)
                .ForeignKey(x => x.OrderId)
                .UseSqlServerTvp("dbo.OrderItemTvp", "dbo.InsertOrderItemsTvp");
        },
        ct);

    return Results.Created($"/orders/{id}", new { Id = id, request.OrderNo, request.GrandTotal, Items = request.Items.Count });
})
.WithTags("17 Graph Insert TVP");


app.MapGet("/orders/status/{status}", async (OrderStatus status, ForgeDbContext db, CancellationToken ct) =>
    Results.Ok(await db.QueryAsync<OrderSummaryRecord>(
        """
        SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
        FROM dbo.Orders
        WHERE Status = @status
        ORDER BY CreatedAt DESC
        """,
        new { status },
        cancellationToken: ct)))
.WithTags("18 Enum Mapping");

app.MapGet("/v2/cache/demo", async (IForgeQueryCache cache) =>
{
    var value = await cache.GetOrCreateAsync("demo:products", _ => Task.FromResult(new { CachedAtUtc = DateTimeOffset.UtcNow, Source = "ForgeORM cache" }), TimeSpan.FromMinutes(5));
    return Results.Ok(value);
})
.WithTags("20 V2 Redis/Distributed Cache");

app.MapPost("/v2/security/validate-sql", (string sql, IForgeSqlSecurityValidator validator) =>
    Results.Ok(validator.Validate(sql)))
.WithTags("21 V2 Security");

app.MapGet("/v2/security/mask-email", (string email, IForgeDataMasker masker) =>
    Results.Ok(new { original = email, masked = masker.MaskEmail(email) }))
.WithTags("21 V2 Security");

app.MapGet("/v2/telemetry/snapshot", (IForgeTelemetry telemetry) =>
    Results.Ok(telemetry.Snapshot()))
.WithTags("22 V2 Telemetry");

app.MapPost("/v3/ai/optimize", (string sql, IForgeAiOptimizer optimizer) =>
    Results.Ok(optimizer.Optimize(new ForgeAiOptimizationRequest(sql))))
.WithTags("23 V3 AI Optimization");

app.MapPost("/v3/ai/diagnose", (string sql, double elapsedMs, int rowCount, IForgeAiDiagnostics diagnostics) =>
    Results.Ok(diagnostics.Diagnose(sql, TimeSpan.FromMilliseconds(elapsedMs), rowCount)))
.WithTags("24 V3 AI Diagnostics");

app.MapPost("/v3/ai/generate-crud", (string entityName, string routePrefix, IForgeAiCodeGenerator generator) =>
    Results.Ok(generator.GenerateMinimalApiCrud(entityName, routePrefix)))
.WithTags("25 V3 AI Code Generation");

app.MapPost("/v3/ai/migration/add-column", (string table, string column, string sqlType, bool nullable, IForgeAiMigrationPlanner planner) =>
    Results.Ok(planner.PlanAddColumn(table, column, sqlType, nullable)))
.WithTags("26 V3 AI Migrations");

app.MapPost("/v3/vector/upsert", async (ForgeVectorDocument document, IForgeVectorStore store) =>
{
    await store.UpsertAsync(document);
    return Results.Ok(new { document.Id, Upserted = true });
})
.WithTags("27 V3 Vector Search");

app.MapPost("/v3/vector/search", async (float[] vector, int topK, IForgeVectorStore store) =>
    Results.Ok(await store.SearchAsync(vector, topK)))
.WithTags("27 V3 Vector Search");

app.Run();

[ForgeTable("Products")]
public sealed class Product { public int Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Price { get; set; } public int? CategoryId { get; set; } public int? BrandId { get; set; } public List<Category> Categories { get; set; } = []; }
[ForgeTable("Categories")]
public sealed class Category { public int Id { get; set; } public string Name { get; set; } = ""; }
[ForgeTable("Brands")]
public sealed class Brand { public int Id { get; set; } public string Name { get; set; } = ""; }
[ForgeTable("Customers")]
public sealed class Customer { public int Id { get; set; } public string Name { get; set; } = ""; public string Email { get; set; } = ""; public CustomerProfile? Profile { get; set; } public List<Order> Orders { get; set; } = []; }
public sealed class CustomerProfile { public int Id { get; set; } public int CustomerId { get; set; } public string Phone { get; set; } = ""; public string City { get; set; } = ""; }
[ForgeTable("Orders")]
public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string OrderNo { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal GrandTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

[ForgeTable("OrderItems")]
public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class ProductCategory { public int ProductId { get; set; } public int CategoryId { get; set; } }
public sealed record ProductListItem(int Id, string Code, string Name, decimal Price, string? CategoryName, string? BrandName);
public sealed record OrderSummaryRecord(int Id, string OrderNo, OrderStatus Status, decimal GrandTotal, DateTimeOffset CreatedAt);
public enum OrderStatus
{
    Draft = 0,
    Pending = 1,
    Paid = 2,
    Shipped = 3,
    Completed = 4,
    Cancelled = 5
}

public sealed class ProductCreateRequest { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Price { get; set; } public int? CategoryId { get; set; } public int? BrandId { get; set; } }
public sealed class CreateOrderRequest
{
    public int CustomerId { get; set; }
    public string OrderNo { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal GrandTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}
public sealed class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
