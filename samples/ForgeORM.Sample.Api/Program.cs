using ForgeORM.QueryAst;
using ForgeORM.Abstractions;
using ForgeORM.AspNetCore;
using ForgeORM.Intelligence;
using ForgeORM.NextGen;
using ForgeORM.QueryBuilder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddForgeOrm(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ForgeORM Sample API v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => "ForgeORM Complete All Features");

app.MapGet("/products/raw", async (IForgeDb db) =>
    await db.QueryAsync<Product>("SELECT * FROM Products WHERE Price > @Price", new { Price = 10 }));

app.MapGet("/products/object-query", async (IForgeDb db) =>
    await db.Set<Product>().Where(x => x.Price > 10).OrderByDescending(x => x.Id).Skip(0).Take(20).ToListAsync());

app.MapGet("/products/{id:int}", async (int id, IForgeDb db) =>
{
    var product = await db.GetByIdAsync<Product>(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapGet("/products/procedure", async (IForgeDb db) =>
    await db.QueryProcedureAsync<Product>("dbo.GetProducts"));

app.MapGet("/query-builder", async (IForgeSelectQueryBuilder qb, IForgeDb db) =>
{
    var q = qb.Select("Id", "Code", "Name", "Price")
        .From("Products")
        .Where("Price > @Price", new { Price = 10 })
        .OrderBy("Id DESC")
        .Skip(0)
        .Take(20)
        .Build();

    return await db.QueryAsync<Product>(q.Sql, q.Parameters);
});

app.MapPost("/sql/intelligence", (SqlRequest request, IForgeSqlIntelligence intelligence) =>
{
    return intelligence.Complete(request.Sql, request.Sql.Length, new ForgeSqlContext
    {
        Tables =
        [
            new ForgeTableSchema { Name = "Products", Columns = ["Id", "Code", "Name", "Price"] }
        ]
    });
});


app.MapGet("/products/nextgen", async (IForgeDb db) =>
{
    return await db.SmartSql<Product>($"SELECT Id, Code, Name, Price FROM Products WHERE Price > {10}")
        .WhereSql($"Name <> {""}")
        .AsCached(TimeSpan.FromMinutes(5))
        .WithPolicy(new ForgeResiliencePolicy { RetryCount = 2, RetryDelay = TimeSpan.FromMilliseconds(100) })
        .ToShapeAsync<Product>();
});

app.MapGet("/products/explain", (IForgeDb db) =>
{
    return db.SmartSql<Product>("SELECT Id, Code, Name, Price FROM Products")
        .Explain();
});

app.MapGet("/schema/verify-products", (IForgeSchemaManager schema) =>
{
    return schema.VerifySchema<Product>();
});


app.MapGet("/nextgen/schema-aware", (IForgeDb db) =>
{
    var command = db.SchemaSql<Product>($"SELECT Id, Code, Name, Price FROM Products WHERE Price > {10}")
        .ExecuteTransparent();

    return Results.Ok(command);
});

app.MapGet("/nextgen/autojoin", async (IForgeDb db) =>
{
    return await db.SmartSql<Product>("SELECT Id, Code, Name, Price FROM Products")
        .AutoJoin()
        .SelectAutomatic()
        .ToListAsync();
});

app.MapGet("/nextgen/trace", (IForgeDb db, IForgeTraceVisualizer visualizer) =>
{
    var trace = db.SmartSql<Product>("SELECT Id, Code, Name, Price FROM Products")
        .TraceVisualizer(visualizer);

    return Results.Ok(trace);
});

app.MapGet("/nextgen/semantic-search", async (IForgeDb db, IForgeSemanticSearch semanticSearch) =>
{
    var query = semanticSearch.SearchSemantic<Product>("Name", "keyboard", top: 10);
    return await db.QueryAsync<Product>(query.Sql, query.Parameters);
});

app.MapGet("/nextgen/reflect-request", async (HttpContext http, IForgeRequestReflector reflector, IForgeDb db) =>
{
    var query = reflector.ReflectRequest<Product>(http);
    return await db.QueryAsync<Product>(query.Sql, query.Parameters);
});

app.MapPost("/products/bulk", async (List<Product> products, IForgeDb db) =>
{
    await db.BulkInsertAsync(products);
    return Results.Ok();
});


app.MapGet("/query-ast", async (IForgeDb db) =>
{
    var minPrice = 10m;

    var query = ForgeSql
        .Select<Product>()
        .Columns(x => x.Id, x => x.Name, x => x.Price)
        .Where(x => x.Price > minPrice)
        .OrderByDescending(x => x.Id)
        .Take(20)
        .Render(db.Provider);

    return await db.QueryAsync<Product>(query.Sql, query.Parameters);
});

app.MapGet("/query-cte", async (IForgeDb db) =>
{
    var query = ForgeSql
        .Select<Product>()
        .WithCte("LatestProducts", """
            SELECT *, ROW_NUMBER() OVER(PARTITION BY Code ORDER BY Id DESC) rn
            FROM Products
        """)
        .From("LatestProducts")
        .WhereSql("rn = 1")
        .Render(db.Provider);

    return await db.QueryAsync<Product>(query.Sql, query.Parameters);
});

app.MapGet("/query-temp-table-script", (IForgeDb db) =>
{
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

    return Results.Ok(script.Sql);
});


app.Run();

public sealed record SqlRequest(string Sql);

[ForgeTable("Products")]
public sealed class Product
{
    [ForgeKey]
    public int Id { get; set; }

    [ForgeCode]
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
