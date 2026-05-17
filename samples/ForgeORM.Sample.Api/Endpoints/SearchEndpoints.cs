using ForgeORM.Core;


public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/search")
            .WithTags("Package Search API");

        group.MapGet("/orders", async (
            int? customerId,
            string? orderNo,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Search<Order>()
                .From("dbo.Orders")
                .Optional(x => x.CustomerId, customerId)
                .OptionalLike(x => x.OrderNo, orderNo)
                .OptionalBetween(x => x.CreatedAt, from, to)
                .OrderByDescending(x => x.CreatedAt)
                .Page(page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize)
                .ToPagedAsync(ct);

            return Results.Ok(result);
        });

        group.MapGet("/orders/sql-style", async (
            int? customerId,
            string? orderNo,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Search<Order>()
                .From("dbo.Orders")
                .Optional("CustomerId", customerId)
                .OptionalLike("OrderNo", orderNo)
                .OrderBy("CreatedAt DESC")
                .Page(1, 50)
                .ToPagedAsync(ct);

            return Results.Ok(result);
        });

        return app;
    }
}
