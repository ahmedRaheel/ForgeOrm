using ForgeORM.Abstractions;
using ForgeORM.AspNetCore;
using ForgeORM.Intelligence;
using ForgeORM.NextGen;
using ForgeORM.QueryBuilder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddForgeOrm(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);
});

var app = builder.Build();

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

app.MapGet("/query-builder", async (IForgeDynamicQueryBuilder qb, IForgeDb db) =>
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

app.MapPost("/products/bulk", async (List<Product> products, IForgeDb db) =>
{
    await db.BulkInsertAsync(products);
    return Results.Ok();
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
