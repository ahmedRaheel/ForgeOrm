using ForgeORM.Core;
using ForgeORM.QueryAst;

public static class SplitQueryEndpoints
{
    public static IEndpointRouteBuilder MapSplitQueryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/split-query").WithTags("05 Split Query / Parent Child Fetching");

        group.MapGet("/one-to-one", async (ForgeDbContext db) =>
        {
            


            var rows = await db.SplitGraph<Customer>()
                .IncludeOne<CustomerProfile, int>(
                    ids => "SELECT * FROM dbo.CustomerProfiles WHERE CustomerId IN @Ids",
                    c => c.Id,
                    p => p.CustomerId,
                    (c, profile) => c.Profile = profile)
                .ToListAsync("SELECT * FROM dbo.Customers");

            return Results.Ok(rows);
        });

        group.MapGet("/one-to-many", async (ForgeDbContext db) =>
        {
            var rows = await db.SplitGraph<Customer>()
                .IncludeMany<Order, int>(
                    ids => "SELECT * FROM dbo.Orders WHERE CustomerId IN @Ids",
                    c => c.Id,
                    o => o.CustomerId,
                    (c, orders) => c.Orders = orders.ToList())
                .ToListAsync("SELECT * FROM dbo.Customers");

            return Results.Ok(rows);
        });

        group.MapGet("/many-to-many", async (ForgeDbContext db) =>
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
        });

        group.MapGet("/paged-customer-orders", async (int page, int pageSize, ForgeDbContext db) =>
        {
            var safePage = Math.Max(page, 1);
            var safeSize = Math.Clamp(pageSize, 1, 200);

            var parentSql = $"""
                SELECT *
                FROM dbo.Customers
                ORDER BY Id DESC
                OFFSET {(safePage - 1) * safeSize} ROWS FETCH NEXT {safeSize} ROWS ONLY
                """;

            var rows = await db.SplitGraph<Customer>()
                .IncludeMany<Order, int>(
                    ids => "SELECT * FROM dbo.Orders WHERE CustomerId IN @Ids",
                    c => c.Id,
                    o => o.CustomerId,
                    (c, orders) => c.Orders = orders.ToList())
                .ToListAsync(parentSql);

            return Results.Ok(new { page = safePage, pageSize = safeSize, rows });
        });


        group.MapGet("/ef-style/customers-with-orders", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Set<Customer>()
                .Include(x => x.Orders)
                .AsSplitQuery()
                .UseIdentityResolution()
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/ef-style/orders-with-items", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Set<Order>()
                .Include(x => x.Items)
                .AsSplitQuery()
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/ef-style/products-with-categories", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Set<Product>()
                .Include(x => x.Categories)
                .AsSplitQuery()
                .UseIdentityResolution()
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        return app;
    }
}
