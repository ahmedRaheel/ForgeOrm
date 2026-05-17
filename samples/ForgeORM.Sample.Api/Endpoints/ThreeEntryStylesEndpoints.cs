using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.Core.EntryStyles;

public static class ThreeEntryStylesEndpoints
{
    public static IEndpointRouteBuilder MapThreeEntryStylesEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/three-entry-styles")
            .WithTags("Three Entry Styles: Builder / SQL / Expression");

        group.MapGet("/query/builder/products", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(20)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.FluentBuilder,
                result
            });
        });

        group.MapGet("/query/sql/products", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Sql(
                    """
                    SELECT TOP (20) *
                    FROM dbo.Products
                    WHERE Price > @MinPrice
                    ORDER BY Id DESC
                    """,
                    new { MinPrice = 100m })
                .ToListAsync<Product>(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.RawSql,
                result
            });
        });

        group.MapGet("/query/expression/products", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Expression<Product>()
                .From("dbo.Products")
                .Where(x => x.Price, ">", 100m)
                .OrderByDescending(x => x.Id)
                .Take(20)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.Expression,
                result
            });
        });

        group.MapGet("/report/builder/top-customers", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomers")
                .From("dbo.Orders")
                .Dimension("CustomerId", "CustomerId")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .TopN("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.FluentBuilder,
                result
            });
        });

        group.MapGet("/report/sql/top-customers", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersSql")
                .From("dbo.Orders")
                .DimensionSql("CustomerId", "CustomerId")
                .SumSql("GrandTotal", "Revenue")
                .TopNSql("Revenue", 10)
                .ToJsonAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.RawSql,
                result
            });
        });

        group.MapGet("/report/expression/top-customers", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("TopCustomersExpression")
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

        group.MapGet("/pivot/builder/sales", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("SalesPivot")
                .From("dbo.Orders")
                .Pivot(
                    row: "YEAR(CreatedAt)",
                    column: "Status",
                    value: "GrandTotal",
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.FluentBuilder,
                result
            });
        });

        group.MapGet("/pivot/sql/sales", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("SalesPivotSql")
                .From("dbo.Orders")
                .PivotSql(
                    row: "YEAR(CreatedAt)",
                    column: "Status",
                    value: "GrandTotal",
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.RawSql,
                result
            });
        });

        group.MapGet("/pivot/expression/sales", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var result = await db.Report<Order>("SalesPivotExpression")
                .From("dbo.Orders")
                .PivotExpr<Order, int, OrderStatus, decimal>(
                    row: x => x.CreatedAt.Year,
                    column: x => x.Status,
                    value: x => x.GrandTotal,
                    aggregate: "SUM",
                    alias: "Revenue")
                .ToDictionaryAsync(ct);

            return Results.Ok(new
            {
                style = ForgeEntryStyle.Expression,
                result
            });
        });

        group.MapGet("/dataframe/sql/products", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var frame = await db.Sql(
                    """
                    SELECT TOP (100) Id, Code, Name, Price
                    FROM dbo.Products
                    ORDER BY Id DESC
                    """)
                .Named("ProductsFrame")
                .ToDataFrameAsync(ct);

            return Results.Ok(frame);
        });

        group.MapGet("/csv/expression/products", async (
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var csv = await db.Expression<Product>()
                .From("dbo.Products")
                .Where(x => x.Price, ">", 100m)
                .OrderByDescending(x => x.Id)
                .Take(20)
                .ToCsvAsync(ct);

            return Results.Text(csv, "text/csv");
        });

        return app;
    }
}
