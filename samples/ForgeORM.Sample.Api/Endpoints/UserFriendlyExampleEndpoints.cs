public static class UserFriendlyExampleEndpoints
{
    public static IEndpointRouteBuilder MapUserFriendlyExampleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/examples/user-friendly").WithTags("90 User Friendly API Examples");

        group.MapGet("/graph-insert-single", () => Results.Ok(new
        {
            title = "Single entity insert",
            description = "Use this when only the parent row should be inserted. Child collections are ignored.",
            code = """
            await db.InsertAsync(product, ct);
            """
        }));

        group.MapGet("/graph-insert-with-children", () => Results.Ok(new
        {
            title = "Parent + all child collections insert",
            description = "Use this when ForgeORM should insert the parent and every discovered child list, similar to EF graph insert.",
            code = """
            var order = Order.Create(customerId, orderNumber);
            order.AddItem(productId: 10, quantity: 2, unitPrice: 1500);
            order.AddItem(productId: 12, quantity: 1, unitPrice: 950);

            await db.InsertGraphAsync(order, ct);
            """
        }));

        group.MapGet("/graph-update-upsert-children", () => Results.Ok(new
        {
            title = "Graph update with child upsert",
            description = "Update parent, update existing children, insert new children, and delete/soft-delete removed children.",
            code = """
            await db.UpdateGraphAsync(order, options =>
            {
                options.IncludeChildren = true;
                options.UseBulkWhenPossible = true;
                options.ChildSyncMode = ForgeChildSyncMode.InsertUpdateDeleteMissing;
            }, ct);
            """
        }));

        group.MapGet("/graph-delete-modes", () => Results.Ok(new
        {
            title = "Delete parent only, child only, or parent with children",
            description = "Use explicit delete modes so a destructive operation is never ambiguous.",
            code = """
            await db.DeleteAsync<Order>(orderId, ct);

            await db.DeleteGraphAsync<Order>(orderId, options =>
            {
                options.IncludeChildren = true;
                options.DeleteMode = ForgeDeleteMode.SoftDelete;
            }, ct);
            """
        }));

        group.MapGet("/query-sql-join-projection", () => Results.Ok(new
        {
            title = "SQL-first QueryAst join projection",
            description = "The SQL version remains available for advanced SQL scenarios.",
            code = """
            var q = ForgeSql.Select<Product>()
                .From("dbo.Products p")
                .InnerJoin("dbo.Categories c", "c.Id = p.CategoryId")
                .LeftJoin("dbo.Brands b", "b.Id = p.BrandId")
                .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName", "b.Name AS BrandName")
                .OrderBySql("p.Id DESC")
                .Take(20)
                .Render(db.Provider);

            return await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters, cancellationToken: ct);
            """
        }));

        group.MapGet("/splitgraph-one-to-many-projection", () => Results.Ok(new
        {
            title = "Split graph one-to-many with projection",
            description = "Parent rows are fetched first, child rows are fetched separately, then attached in memory.",
            code = """
            var rows = await db.SplitGraph<Customer>()
                .IncludeMany<Order, int>(
                    ids => "SELECT * FROM dbo.Orders WHERE CustomerId IN @Ids",
                    c => c.Id,
                    o => o.CustomerId,
                    (c, orders) => c.Orders = orders.ToList())
                .ToListAsync("SELECT * FROM dbo.Customers");
            """
        }));

        group.MapGet("/dataframe-pivot", () => Results.Ok(new
        {
            title = "DataFrame pivot",
            description = "Use DataFrame when query results need pandas-like grouping, pivoting, importing, exporting, or analytics.",
            code = """
            var frame = await db.Frame<Order>()
                .FromSql("SELECT YEAR(CreatedAt) AS CreatedYear, Status, GrandTotal FROM dbo.Orders")
                .ToFrameAsync(ct);

            var pivot = frame.PivotTable(
                rows: "CreatedYear",
                columns: "Status",
                values: "GrandTotal",
                aggregate: ForgeAgg.Sum());
            """
        }));

        return app;
    }
}
