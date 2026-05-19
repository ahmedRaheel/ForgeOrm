using ForgeORM.Core;

public static class StoredProcedureFunctionEndpoints
{
    public static IEndpointRouteBuilder MapStoredProcedureFunctionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/database-routines").WithTags("02 Stored Procedures / Functions / QueryMultiple");

        group.MapGet("/stored-procedure/products", async (decimal minPrice, ForgeDbContext db, CancellationToken ct) =>
            Results.Ok(await db.QueryProcedureAsync<ProductListItem>(
                "dbo.sp_GetProductList",
                new { MinPrice = minPrice },
                cancellationToken: ct)));

        group.MapGet("/function/product-count", async (ForgeDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ExecuteScalarAsync<int>("SELECT dbo.fn_ProductCount()", cancellationToken: ct)));

        group.MapGet("/query-multiple/dashboard", async (ForgeDbContext db) =>
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
        });

        return app;
    }
}
