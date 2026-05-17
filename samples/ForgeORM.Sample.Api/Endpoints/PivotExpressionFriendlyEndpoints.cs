using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;

public static class PivotExpressionFriendlyEndpoints
{
    public static IEndpointRouteBuilder MapPivotExpressionFriendlyEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pivot-expression-friendly")
            .WithTags("Pivot Expression Friendly");

        group.MapGet("/sales-pivot/dictionary", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.Report<Order>("SalesPivotExpression")
                .From("dbo.Orders")
                .PivotExpr(
                    row: x => x.CreatedAt.Year,
                    column: x => x.Status,
                    value: x => x.GrandTotal,
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/sales-pivot/dictionary-explicit-value-types", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            // Also valid if the developer wants explicit result type arguments.
            // Note: do not include Order here because db.Report<Order>() already supplies T.
            var rows = await db.Report<Order>("SalesPivotExpressionExplicit")
                .From("dbo.Orders")
                .Pivot<int, OrderStatus, decimal>(
                    row: x => x.CreatedAt.Year,
                    column: x => x.Status,
                    value: x => x.GrandTotal,
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(rows);
        });

        return app;
    }
}
