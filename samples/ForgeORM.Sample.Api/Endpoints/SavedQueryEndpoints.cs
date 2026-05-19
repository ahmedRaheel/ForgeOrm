using ForgeORM.Core;

public static class SavedQueryEndpoints
{
    public static IEndpointRouteBuilder MapSavedQueryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/saved-queries")
            .WithTags("19 Saved Queries / Expression Query Builder");

        group.MapPost("/register-high-value-orders-lambda", async (ForgeDbContext db) =>
        {
            await db.SavedQueries.Register("HighValueOrders", query =>
            {
                query.From<Order>()
                    .Where(x => x.TotalAmount > 10000)
                    .OrderByDescending(x => x.CreatedAt);
            });

            return Results.Ok(new { registered = true, name = "HighValueOrders", mode = "lambda-root" });
        });

        group.MapPost("/register-high-value-orders-typed", async (ForgeDbContext db) =>
        {
            await db.SavedQueries.Register<Order>("HighValueOrdersTyped", query =>
            {
                query.From<Order>()
                    .Where(x => x.TotalAmount > 10000)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50);
            });

            return Results.Ok(new { registered = true, name = "HighValueOrdersTyped", mode = "typed-lambda" });
        });

        group.MapPost("/register-raw-sql", (ForgeDbContext db) =>
        {
            db.SavedQueries.Register(
                name: "HighValueOrdersSql",
                sql: """
SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
FROM dbo.Orders
WHERE GrandTotal >= @MinTotal
ORDER BY CreatedAt DESC
""",
                parameters: new { MinTotal = 1000m },
                description: "Reusable high-value order list using raw SQL.");

            return Results.Ok(new { registered = true, name = "HighValueOrdersSql", mode = "raw-sql" });
        });

        group.MapGet("/query-builder/products/sql", (ForgeDbContext db) =>
        {
            var rendered = db.Query<Product>()
                .From<Product>()
                .Select(x => x.Id, x => x.Code, x => x.Name, x => x.Price)
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(20)
                .Render();

            return Results.Ok(new
            {
                rendered.Sql,
                rendered.Parameters
            });
        });

        group.MapGet("/query-builder/products/execute", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From<Product>()
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(20)
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/list", (ForgeDbContext db) =>
        {
            return Results.Ok(db.SavedQueries.List().Select(x => new
            {
                x.Name,
                x.Description,
                x.CreatedAtUtc,
                x.Sql
            }));
        });

        group.MapGet("/execute/{name}", async (
            string name,
            decimal? minTotal,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            var rows = await db.SavedQueries.ExecuteAsync<OrderSummaryRecord>(
                name,
                minTotal.HasValue ? new { MinTotal = minTotal.Value } : null,
                ct);

            return Results.Ok(rows);
        });

        group.MapGet("/orders/high-value", async (
            decimal minTotal,
            ForgeDbContext db,
            CancellationToken ct) =>
        {
            if (!db.SavedQueries.List().Any(x => x.Name == "HighValueOrdersSql"))
            {
                db.SavedQueries.Register(
                    name: "HighValueOrdersSql",
                    sql: """
SELECT Id, OrderNo, Status, GrandTotal, CreatedAt
FROM dbo.Orders
WHERE GrandTotal >= @MinTotal
ORDER BY CreatedAt DESC
""",
                    description: "Reusable high-value order list.");
            }

            var rows = await db.SavedQueries.ExecuteAsync<OrderSummaryRecord>(
                "HighValueOrdersSql",
                new { MinTotal = minTotal },
                ct);

            return Results.Ok(rows);
        });

        group.MapDelete("/{name}", (string name, ForgeDbContext db) =>
        {
            var removed = db.SavedQueries.Remove(name);
            return Results.Ok(new
            {
                Name = name,
                Removed = removed
            });
        });

        return app;
    }
}
