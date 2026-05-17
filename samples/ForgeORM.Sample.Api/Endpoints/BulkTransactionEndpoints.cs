using ForgeORM.Core;

public static class BulkTransactionEndpoints
{
    public static IEndpointRouteBuilder MapBulkTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bulk-transactions").WithTags("04 Bulk / Transactions / DTO Insert");

        group.MapPost("/products", async (List<ProductCreateRequest> rows, ForgeDbContext db, CancellationToken ct) =>
        {
            await db.BulkInsertAsync("dbo.Products", rows, cancellationToken: ct);
            return Results.Ok(new { Inserted = rows.Count });
        });

        group.MapPost("/orders/insert-dto", async (CreateOrderRequest request, ForgeDbContext db, CancellationToken ct) =>
        {
            var inserted = await db.InsertAsync<Order, CreateOrderRequest>(request, ct);
            return Results.Ok(new { Inserted = inserted });
        });

        group.MapPost("/increase-prices", async (decimal amount, ForgeDbContext db) =>
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
        });

        return app;
    }
}
