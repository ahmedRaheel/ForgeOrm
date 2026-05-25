using ForgeORM.Core;

public static class PerformancePipelineEndpoints
{
    public static IEndpointRouteBuilder MapPerformancePipelineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/performance")
            .WithTags("Performance / MSIL Hot Path");

        group.MapPost("/prewarm", (ForgeDb db) =>
        {
            db.PreWarm(typeof(Product), typeof(Category), typeof(Brand), typeof(Customer), typeof(Order), typeof(OrderItem));
            return Results.Ok(new
            {
                warmed = true,
                entities = new[] { "Product", "Category", "Brand", "Customer", "Order", "OrderItem" },
                message = "Entity metadata, MSIL getters/setters and SQL fragments are built once and reused from ConcurrentDictionary caches."
            });
        });

        group.MapGet("/stats", (ForgeDb db) => Results.Ok(db.GetPerformanceCacheStats()));

        group.MapGet("/products/query-fast", async (ForgeDb db, CancellationToken ct) =>
        {
            var rows = await db.QueryFastAsync<Product>(
                "SELECT Id, Code, Name, Price, CategoryId, BrandId FROM Products WHERE Price >= @MinPrice ORDER BY Id",
                new { MinPrice = 0m },
                cancellationToken: ct);

            return Results.Ok(rows);
        });

        group.MapGet("/products/stream", async (ForgeDb db, HttpResponse response, CancellationToken ct) =>
        {
            response.ContentType = "application/json";
            await response.WriteAsync("[", ct);
            var first = true;

            await foreach (var product in db.QueryStreamAsync<Product>(
                "SELECT Id, Code, Name, Price, CategoryId, BrandId FROM Products ORDER BY Id",
                cancellationToken: ct))
            {
                if (!first)
                    await response.WriteAsync(",", ct);

                first = false;
                await response.WriteAsJsonAsync(product, cancellationToken: ct);
            }

            await response.WriteAsync("]", ct);
        });

        group.MapGet("/products/{id:int}/compiled", async (int id, ForgeDb db, CancellationToken ct) =>
        {
            var product = await db.GetByIdCompiledAsync<Product>(id, ct);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        //group.MapPost("/products/compiled-insert", async (ProductCreateRequest request, ForgeDb db, CancellationToken ct) =>
        //{
        //    var product = new Product
        //    {
        //        Code = request.Code,
        //        Name = request.Name,
        //        Price = request.Price,
        //        CategoryId = request.CategoryId,
        //        BrandId = request.BrandId
        //    };

        //    var affected = await db.InsertCompiledAsync(product, ct);
        //    return Results.Ok(new { affected, mode = "MSIL parameter binder + cached insert SQL" });
        //});

        group.MapPut("/products/{id:int}/compiled-update", async (int id, ProductCreateRequest request, ForgeDb db, CancellationToken ct) =>
        {
            var product = new Product
            {
                Id = id,
                Code = request.Code,
                Name = request.Name,
                Price = request.Price,
                CategoryId = request.CategoryId,
                BrandId = request.BrandId
            };

            var affected = await db.UpdateCompiledAsync(product, ct);
            return Results.Ok(new { affected, mode = "MSIL parameter binder + cached update SQL" });
        });

        group.MapDelete("/products/{id:int}/compiled-delete", async (int id, ForgeDb db, CancellationToken ct) =>
        {
            var affected = await db.DeleteCompiledAsync<Product>(id, ct);
            return Results.Ok(new { affected, mode = "cached delete SQL" });
        });

        group.MapGet("/orders/temporal/as-of", async (DateTime asOfUtc, ForgeDb db, CancellationToken ct) =>
        {
            var rows = await db.TemporalAsOfAsync<Order>(asOfUtc, cancellationToken: ct);
            return Results.Ok(rows);
        });

        group.MapPost("/products/bulk-hook", async (List<Product> products, ForgeDb db, CancellationToken ct) =>
        {
            await db.BulkInsertAsync(products, ct);
            return Results.Ok(new { inserted = products.Count, mode = "provider bulk extension point" });
        });

        group.MapPost("/orders/graph-hook", async (CreateOrderRequest request, ForgeDb db, CancellationToken ct) =>
        {
            var order = new Order
            {
                CustomerId = request.CustomerId,
                OrderNo = request.OrderNo,
                Status = request.Status,
                GrandTotal = request.GrandTotal,
                CreatedAt = request.CreatedAt,
                OrderDate = request.OrderDate,
                TotalAmount = request.TotalAmount,
                Items = request.Items.Select(x => new OrderItem
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    LineTotal = x.LineTotal
                }).ToList()
            };

            var result = await db.InsertGraphAsync(order, cancellationToken: ct);
            return Results.Ok(new { result, mode = "graph extension point" });
        });

        return app;
    }
}
