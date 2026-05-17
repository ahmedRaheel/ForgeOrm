using ForgeORM.Abstractions;
using ForgeORM.Core;
using ForgeORM.QueryAst;

public static class QueryBuilderEndpoints
{
    public static IEndpointRouteBuilder MapQueryBuilderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/query-builder").WithTags("03 Query Builder / AST / Pagination / CTE / Temp Table");

        group.MapGet("/string/products", async (decimal minPrice, IForgeDynamicQueryBuilder qb, ForgeDbContext db, CancellationToken ct) =>
        {
            var q = qb.Select("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName", "b.Name AS BrandName")
                .From("dbo.Products p")
                .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
                .LeftJoin("dbo.Brands b", "b.Id = p.BrandId")
                .Where("p.Price > @MinPrice", new { MinPrice = minPrice })
                .OrderBy("p.Id DESC")
                .Take(20)
                .Build(db.Provider);

            return Results.Ok(await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters, cancellationToken: ct));
        });

        group.MapGet("/ast/products", async (decimal minPrice, ForgeDbContext db, CancellationToken ct) =>
        {
            var q = ForgeSql.Select<Product>()
                .From("dbo.Products p")
                .LeftJoin<Category>((p, c) => p.CategoryId == c.Id)
                .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
                .WhereSql("p.Price > @MinPrice", new { MinPrice = minPrice })
                .OrderBySql("p.Id DESC")
                .Take(20)
                .Render(db.Provider);

            return Results.Ok(await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters, cancellationToken: ct));
        });

        group.MapGet("/ast/all-joins", async (ForgeDbContext db, CancellationToken ct) =>
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

            return Results.Ok(await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters, cancellationToken: ct));
        });

        group.MapGet("/cte/latest-products", async (ForgeDbContext db, CancellationToken ct) =>
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

            return Results.Ok(await db.QueryAsync<Product>(q.Sql, q.Parameters, cancellationToken: ct));
        });

        group.MapGet("/temp-table/script", (ForgeDbContext db) =>
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

            return Results.Ok(new { script.Sql });
        });

        group.MapGet("/pagination/products", async (int page, int pageSize, ForgeDbContext db, CancellationToken ct) =>
        {
            var q = ForgeSql.Select<Product>()
                .From("dbo.Products")
                .Columns(x => x.Id, x => x.Code, x => x.Name, x => x.Price)
                .OrderByDescending(x => x.Id)
                .Skip(Math.Max(page - 1, 0) * pageSize)
                .Take(pageSize)
                .Render(db.Provider);

            return Results.Ok(await db.QueryAsync<Product>(q.Sql, q.Parameters, cancellationToken: ct));
        });

        group.MapGet("/set-operations/union-all", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var q = ForgeSql.Select<Product>()
                .Columns(p => p.Id, p => p.Code, p => p.Name)
                .Where(p => p.Price > 100)
                .UnionAll(x => x
                    .Columns(p => p.Id, p => p.Code, p => p.Name)
                    .Where(p => p.Price < 10))
                .Render(db.Provider);

            return Results.Ok(await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters, cancellationToken: ct));
        });

        group.MapGet("/group-having/aggregate", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var q = ForgeSql.Select<Order>()
                .Columns(o => o.CustomerId)
                .Count("OrderCount")
                .Sum(o => o.TotalAmount, "TotalSales")
                .Average(o => o.TotalAmount, "AverageOrderValue")
                .Min(o => o.TotalAmount, "SmallestOrder")
                .Max(o => o.TotalAmount, "LargestOrder")
                .GroupBy(o => o.CustomerId)
                .HavingSum(o => o.TotalAmount, ">", 5000m)
                .OrderByDescending(o => o.CustomerId)
                .Render(db.Provider);

            return Results.Ok(await db.QueryAsync<CustomerOrderAggregateDto>(q.Sql, q.Parameters, cancellationToken: ct));
        });

        return app;
    }
}
