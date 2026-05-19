using ForgeORM.Core;

public static class PerformanceShowcaseEndpoints
{
    public static IEndpointRouteBuilder MapPerformanceShowcaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/performance").WithTags("Performance / MSIL / Source Generators");

        group.MapGet("/orders/msil-query", async (ForgeDbContext db, CancellationToken ct) =>
        {
            const string sql = "select top 50 Id, OrderNo, CustomerId, GrandTotal, Status from Orders order by Id desc";
            var rows = await db.QueryAsync<Order>(sql, cancellationToken: ct);
            return Results.Ok(new { engine = "Source-generated reader when registered, otherwise cached MSIL reader", count = rows.Count, rows });
        });

        group.MapGet("/orders/stream", async (ForgeDbContext db, CancellationToken ct) =>
        {
            const string sql = "select Id, OrderNo, CustomerId, GrandTotal, Status from Orders order by Id";
            var count = 0;
            await foreach (var _ in db.StreamAsync<Order>(sql, cancellationToken: ct))
                count++;
            return Results.Ok(new { streamed = count, mode = "SequentialAccess + cached materializer" });
        });

        group.MapPost("/orders/parameters", async (ForgeDbContext db, OrderSearchRequest request, CancellationToken ct) =>
        {
            const string sql = "select Id, OrderNo, CustomerId, GrandTotal, Status from Orders where CustomerId = @CustomerId and Status = @Status";
            var rows = await db.QueryAsync<Order>(sql, request, cancellationToken: ct);
            return Results.Ok(new { binder = "source-generated binder when registered, otherwise cached MSIL binder", rows });
        });

        return app;
    }
}

public sealed class OrderSearchRequest
{
    public int CustomerId { get; set; }
    public string Status { get; set; } = "Paid";
}
