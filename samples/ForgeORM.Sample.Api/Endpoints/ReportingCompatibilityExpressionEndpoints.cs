using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.Core.EntryStyles;

public static class ReportingCompatibilityExpressionEndpoints
{
    public static IEndpointRouteBuilder MapReportingCompatibilityExpressionEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/report-compatibility")
            .WithTags("Report Compatibility Expression Overloads");

        group.MapGet("/explicit-generics", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersCompatibilityExplicit")
                .From("dbo.Orders")
                .DimensionExpr<Order, int>(x => x.CustomerId)
                .SumExpr<Order, decimal>(x => x.GrandTotal, "Revenue")
                .TopNSql("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.Expression,
                result
            });
        });

        group.MapGet("/clean-inference", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersCompatibilityClean")
                .From("dbo.Orders")
                .DimensionExpr(x => x.CustomerId)
                .SumExpr(x => x.GrandTotal, "Revenue")
                .TopNSql("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.Expression,
                result
            });
        });

        group.MapGet("/alias-inference", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersCompatibilityAlias")
                .From("dbo.Orders")
                .Dimension("CustomerId", x => x.CustomerId)
                .Sum(x => x.GrandTotal, "Revenue")
                .TopNSql("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.Expression,
                result
            });
        });

        return app;
    }
}
