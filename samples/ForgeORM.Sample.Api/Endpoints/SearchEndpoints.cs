using ForgeORM.Core;
using ForgeORM.Core.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/search").WithTags("07 Universal Search / Dynamic Filters / Procedures");

        group.MapGet("/products/text", async (
            string? code,
            string? name,
            decimal? minPrice,
            decimal? maxPrice,
            int page,
            int pageSize,
            ForgeDbContext db) =>
        {
            return Results.Ok(await db.Search<Product>()
                .FromSql("SELECT Id, Code, Name, Price FROM dbo.Products")
                .WhereIf(!string.IsNullOrWhiteSpace(code), "Code = @Code", new { Code = code })
                .WhereIf(!string.IsNullOrWhiteSpace(name), "Name LIKE @Name", new { Name = $"%{name}%" })
                .WhereIf(minPrice.HasValue, "Price >= @MinPrice", new { MinPrice = minPrice })
                .WhereIf(maxPrice.HasValue, "Price <= @MaxPrice", new { MaxPrice = maxPrice })
                .OrderByDescending(x => x.Id)
                .Page(page, pageSize)
                .ToPagedAsync());
        });

        group.MapGet("/products/expression", async (
            decimal? minPrice,
            decimal? maxPrice,
            int page,
            int pageSize,
            ForgeDbContext db) =>
        {
            return Results.Ok(await db.Search<Product>()
                .From("dbo.Products")
                .WhereIf(minPrice.HasValue, x => x.Price >= minPrice!.Value)
                .WhereIf(maxPrice.HasValue, x => x.Price <= maxPrice!.Value)
                .OrderByDescending(x => x.Id)
                .Page(page, pageSize)
                .ToPagedAsync());
        });

        group.MapGet("/products/builder", async (
            string? code,
            string? name,
            decimal? minPrice,
            decimal? maxPrice,
            int page,
            int pageSize,
            ForgeDbContext db) =>
        {
            return Results.Ok(await db.Search<Product>()
                .Select(x => x.Id, x => x.Code, x => x.Name, x => x.Price)
                .From("dbo.Products")
                .Optional(x => x.Code, code)
                .OptionalLike(x => x.Name, name)
                .OptionalBetween(x => x.Price, minPrice, maxPrice)
                .OrderByDescending(x => x.Id)
                .Page(page, pageSize)
                .ToPagedAsync());
        });

        group.MapGet("/products/procedure", async (
            string? code,
            string? name,
            decimal? minPrice,
            decimal? maxPrice,
            int page,
            int pageSize,
            ForgeDbContext db) =>
        {
            return Results.Ok(await db.SearchProcedure<Product>("dbo.SearchProducts")
                .WithOptional("Code", code)
                .WithOptional("Name", name)
                .WithOptional("MinPrice", minPrice)
                .WithOptional("MaxPrice", maxPrice)
                .Page(page, pageSize)
                .ToListAsync());
        });

        group.MapGet("/profiled/products", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var result = await db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(50)
                .Profile("HighValueProducts")
                .ToListAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/analyze-index/products", (ForgeDbContext db) =>
        {
            var analysis = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.CategoryId == 10)
                .OrderByDescending(x => x.Id)
                .Analyze();

            return Results.Ok(analysis);
        });

        group.MapGet("/profiled/snapshot", () =>
        {
            return Results.Ok(ForgeQueryProfiler.Snapshot());
        });

        group.MapDelete("/profiled/snapshot", () =>
        {
            ForgeQueryProfiler.Clear();
            return Results.Ok(new { cleared = true });
        });

        group.MapPost("/saved/register-high-value-orders", async (ForgeDbContext db) =>
        {
            await db.SavedQueries.Register("HighValueOrders", query =>
            {
                query.From<Order>()
                    .Where(x => x.TotalAmount > 10000)
                    .OrderByDescending(x => x.CreatedAt);
            });

            return Results.Ok(new { registered = true, name = "HighValueOrders" });
        });

        group.MapGet("/saved/high-value-orders", async (ForgeDbContext db) =>
            Results.Ok(await db.SavedQueries.ExecuteAsync<OrderSummaryRecord>("HighValueOrders")));

        return app;
    }
}
