using ForgeORM.Core;

public static class RawSqlEndpoints
{
    public static IEndpointRouteBuilder MapRawSqlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/raw").WithTags("01 Raw SQL");

        group.MapGet("/products", async (ForgeDbContext db, CancellationToken ct) =>
            Results.Ok(await db.QueryAsync<Product>("SELECT * FROM dbo.Products", cancellationToken: ct)));

        group.MapGet("/products/{id:int}", async (int id, ForgeDbContext db, CancellationToken ct) =>
            Results.Ok(await db.QuerySingleOrDefaultAsync<Product>(
                "SELECT * FROM dbo.Products WHERE Id = @Id",
                new { Id = id },
                cancellationToken: ct)));

        group.MapGet("/orders/customer/{customerId:int}/summary-records", async (int customerId, ForgeDbContext db, CancellationToken ct) =>
            Results.Ok(await db.QueryAsync<OrderSummaryRecord>(
                """
                SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
                FROM dbo.Orders
                WHERE CustomerId = @customerId
                ORDER BY CreatedAt DESC
                """,
                new { customerId },
                cancellationToken: ct)));

        group.MapGet("/orders/status/{status}", async (OrderStatus status, ForgeDbContext db, CancellationToken ct) =>
            Results.Ok(await db.QueryAsync<OrderSummaryRecord>(
                """
                SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
                FROM dbo.Orders
                WHERE Status = @status
                ORDER BY CreatedAt DESC
                """,
                new { status = (int)status },
                cancellationToken: ct)));

        return app;
    }
}
