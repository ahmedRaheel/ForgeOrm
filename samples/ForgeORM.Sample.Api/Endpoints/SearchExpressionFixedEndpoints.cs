using ForgeORM.Core;
public static class SearchExpressionFixedEndpoints
{
    public static IEndpointRouteBuilder MapSearchExpressionFixedEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/search-expression-fixed")
            .WithTags("Search Expression Fixed");

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

        return app;
    }
}
