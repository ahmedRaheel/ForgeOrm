using ForgeORM.Core;
using ForgeORM.Core.Graph;

public static class GraphPersistenceEndpoints
{
    public static IEndpointRouteBuilder MapGraphPersistenceEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/graph-persistence")
            .WithTags("08 Graph Insert / Update / Delete");

        var insert = group.MapGroup("/insert").WithTags("08.1 Graph Insert Examples");
        insert.MapPost("/product/single-entity", InsertSingleProductEntityAsync);
        insert.MapPost("/product/dto", InsertProductDtoAsync);
        insert.MapPost("/product/many", InsertManyProductsAsync);
        insert.MapPost("/orders/entity-auto-graph", InsertOrderEntityAutoGraphAsync);
        insert.MapPost("/orders/dto-auto", InsertOrderDtoAutoAsync);
        insert.MapPost("/orders/dto-tvp", InsertOrderDtoUsingTvpAsync);
        insert.MapPost("/orders/dto-openjson", InsertOrderDtoUsingOpenJsonAsync);
        insert.MapPost("/orders/entity-expression", InsertOrderEntityExpressionAsync);
        insert.MapPost("/orders/dto-factory", InsertOrderDtoUsingFactoryAsync);
        insert.MapPost("/orders/parent-options-only", InsertOrderParentOnlyUsingOptionsAsync);

        var update = group.MapGroup("/update").WithTags("08.2 Graph Update Examples");
        update.MapPut("/orders/single-parent", UpdateSingleOrderParentAsync);
        update.MapPut("/orders/graph-default", UpdateOrderGraphDefaultAsync);
        update.MapPut("/orders/graph-delete-missing", UpdateOrderGraphDeleteMissingAsync);
        update.MapPut("/orders/graph-options-insert-update", UpdateOrderGraphInsertUpdateAsync);
        update.MapPut("/orders/graph-options-sync-delete-missing", UpdateOrderGraphInsertUpdateDeleteMissingAsync);
        update.MapPatch("/products/by-condition", UpdateProductsByConditionAsync);
        update.MapPatch("/products/by-condition-sql", UpdateProductsByConditionSqlAsync);

        var delete = group.MapGroup("/delete").WithTags("08.3 Graph Delete Examples");
        delete.MapDelete("/orders/{id:int}/parent-only", DeleteOrderParentOnlyAsync);
        delete.MapDelete("/orders/{id:int}/graph-hard", DeleteOrderGraphHardAsync);
        delete.MapPost("/orders/graph-hard-by-entity", DeleteOrderGraphHardByEntityAsync);
        delete.MapDelete("/orders/{id:int}/graph-soft", DeleteOrderGraphSoftAsync);
        delete.MapPost("/orders/graph-soft-by-entity", DeleteOrderGraphSoftByEntityAsync);
        delete.MapDelete("/products/by-condition", DeleteProductsByConditionAsync);

        return app;
    }

    private static async ValueTask<IResult> InsertSingleProductEntityAsync(
        ProductCreateRequest request,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var product = new Product
        {
            Code = request.Code,
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId
        };

        var affected = await db.InsertAsync(product, ct);
        return Results.Ok(new { Inserted = affected, product.Code, product.Name, product.Price });
    }

    private static async ValueTask<IResult> InsertProductDtoAsync(
        ProductCreateRequest request,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.InsertAsync<Product, ProductCreateRequest>(request, ct);
        return Results.Ok(new { Inserted = affected, request.Code, request.Name, request.Price });
    }

    private static async ValueTask<IResult> InsertManyProductsAsync(
        List<ProductCreateRequest> rows,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var products = rows.Select(x => new Product
        {
            Code = x.Code,
            Name = x.Name,
            Price = x.Price,
            CategoryId = x.CategoryId,
            BrandId = x.BrandId
        }).ToList();

        var affected = await db.InsertManyAsync(products, ct);
        return Results.Ok(new { Inserted = affected, Rows = products.Count });
    }

    private static async ValueTask<IResult> InsertOrderEntityAutoGraphAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(order);
        var inserted = await db.InsertGraphAsync(order, ct);

        return Results.Created($"/orders/{inserted.Id}", new
        {
            inserted.Id,
            inserted.OrderNo,
            inserted.GrandTotal,
            Items = inserted.Items.Count,
            Strategy = "Auto entity graph"
        });
    }

    private static async ValueTask<IResult> InsertOrderDtoAutoAsync(
        CreateOrderRequest request,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(request);

        var id = await db.InsertGraphAsync<Order, CreateOrderRequest, int>(
            request,
            graph =>
            {
                graph.IncludeChildren = true;
                graph.Strategy = ForgeBulkStrategy.Auto;

                graph.Parent()
                    .Key(x => x.Id);

                graph.Children<OrderItem, CreateOrderItemRequest>(x => x.Items)
                    .ForeignKey(x => x.OrderId);
            },
            ct);

        return Results.Created($"/orders/{id}", new
        {
            Id = id,
            request.OrderNo,
            request.GrandTotal,
            Items = request.Items.Count,
            Strategy = "Auto child mapping"
        });
    }

    private static async ValueTask<IResult> InsertOrderDtoUsingTvpAsync(
        CreateOrderRequest request,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(request);

        var id = await db.InsertGraphAsync<Order, CreateOrderRequest, int>(
            request,
            graph =>
            {
                graph.IncludeChildren = true;
                graph.UseBulkWhenPossible = true;
                graph.Strategy = ForgeBulkStrategy.TableValuedParameter;

                graph.Parent()
                    .Key(x => x.Id);

                graph.Children<OrderItem, CreateOrderItemRequest>(x => x.Items)
                    .ForeignKey(x => x.OrderId)
                    .UseSqlServerTvp(
                        tableType: "dbo.OrderItemTvp",
                        procedure: "dbo.InsertOrderItemsTvp");
            },
            ct);

        return Results.Created($"/orders/{id}", new
        {
            Id = id,
            request.OrderNo,
            request.GrandTotal,
            Items = request.Items.Count,
            Strategy = "SQL Server TVP"
        });
    }

    private static async ValueTask<IResult> InsertOrderDtoUsingOpenJsonAsync(
        CreateOrderRequest request,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(request);

        var id = await db.InsertGraphAsync<Order, CreateOrderRequest, int>(
            request,
            graph =>
            {
                graph.IncludeChildren = true;
                graph.UseBulkWhenPossible = true;
                graph.Strategy = ForgeBulkStrategy.OpenJson;

                graph.Parent()
                    .Key(x => x.Id);

                graph.Children<OrderItem, CreateOrderItemRequest>(x => x.Items)
                    .ForeignKey(x => x.OrderId)
                    .UseSqlServerOpenJson();
            },
            ct);

        return Results.Created($"/orders/{id}", new
        {
            Id = id,
            request.OrderNo,
            request.GrandTotal,
            Items = request.Items.Count,
            Strategy = "SQL Server OPENJSON"
        });
    }

    private static async ValueTask<IResult> InsertOrderEntityExpressionAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(order);

        var id = await db.InsertGraphAsync<Order, OrderItem, int>(
            order,
            x => x.Items,
            x => x.Id,
            x => x.OrderId,
            ct);

        return Results.Created($"/orders/{id}", new
        {
            Id = id,
            order.OrderNo,
            order.GrandTotal,
            Items = order.Items.Count,
            Strategy = "Expression parent-child"
        });
    }

    private static async ValueTask<IResult> InsertOrderDtoUsingFactoryAsync(
        CreateOrderRequest request,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(request);

        var id = await db.InsertGraphAsync<CreateOrderRequest, Order, CreateOrderItemRequest, OrderItem, int>(
            request,
            dto => new Order
            {
                CustomerId = dto.CustomerId,
                OrderNo = dto.OrderNo,
                Status = dto.Status,
                GrandTotal = dto.GrandTotal,
                TotalAmount = dto.TotalAmount,
                CreatedAt = dto.CreatedAt,
                OrderDate = dto.OrderDate
            },
            dto => dto.Items,
            (_, item) => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            },
            x => x.Id,
            x => x.OrderId,
            ct);

        return Results.Created($"/orders/{id}", new
        {
            Id = id,
            request.OrderNo,
            request.GrandTotal,
            Items = request.Items.Count,
            Strategy = "DTO factory mapping"
        });
    }

    private static async ValueTask<IResult> InsertOrderParentOnlyUsingOptionsAsync(
        CreateOrderRequest request,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(request);

        Action<ForgeGraphOptions> configure = options =>
        {
            options.IncludeChildren = false;
            options.UseBulkWhenPossible = false;
            options.Strategy = ForgeBulkStrategy.RowByRow;
        };

        var id = await db.InsertGraphAsync<Order, CreateOrderRequest, int>(
            request,
            configure,
            ct);

        return Results.Created($"/orders/{id}", new
        {
            Id = id,
            request.OrderNo,
            request.GrandTotal,
            Strategy = "Options-based parent only"
        });
    }

    private static async ValueTask<IResult> UpdateSingleOrderParentAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(order);
        var affected = await db.UpdateAsync(order, ct);

        return Results.Ok(new { Updated = affected, order.Id, Mode = "Single parent update" });
    }

    private static async ValueTask<IResult> UpdateOrderGraphDefaultAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(order);
        var affected = await db.UpdateGraphAsync(order, deleteMissingChildren: false, ct);

        return Results.Ok(new { Updated = affected, order.Id, Children = order.Items.Count, Mode = "Graph update" });
    }

    private static async ValueTask<IResult> UpdateOrderGraphDeleteMissingAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(order);
        var affected = await db.UpdateGraphAsync(order, deleteMissingChildren: true, ct);

        return Results.Ok(new { Updated = affected, order.Id, Children = order.Items.Count, Mode = "Graph update delete missing children" });
    }

    private static async ValueTask<IResult> UpdateOrderGraphInsertUpdateAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(order);

        var affected = await db.UpdateGraphAsync(
            order,
            options =>
            {
                options.IncludeChildren = true;
                options.UseBulkWhenPossible = true;
                options.ChildSyncMode = ForgeChildSyncMode.InsertUpdate;
            },
            ct);

        return Results.Ok(new { Updated = affected, order.Id, Children = order.Items.Count, Mode = "Insert/update children" });
    }

    private static async ValueTask<IResult> UpdateOrderGraphInsertUpdateDeleteMissingAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        NormalizeOrderTotals(order);

        var affected = await db.UpdateGraphAsync(
            order,
            options =>
            {
                options.IncludeChildren = true;
                options.UseBulkWhenPossible = true;
                options.ChildSyncMode = ForgeChildSyncMode.InsertUpdateDeleteMissing;
            },
            ct);

        return Results.Ok(new { Updated = affected, order.Id, Children = order.Items.Count, Mode = "Insert/update/delete missing children" });
    }

    private static async ValueTask<IResult> UpdateProductsByConditionAsync(
        decimal maxPrice,
        decimal newPrice,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.UpdateByConditionAsync<Product>(
            new { Price = newPrice },
            p => p.Price <= maxPrice,
            ct);

        return Results.Ok(new { Updated = affected, maxPrice, newPrice });
    }

    private static async ValueTask<IResult> UpdateProductsByConditionSqlAsync(
        decimal minPrice,
        decimal newPrice,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.UpdateByConditionSqlAsync<Product>(
            new { Price = newPrice },
            "Price < @MinPrice",
            new { MinPrice = minPrice },
            ct);

        return Results.Ok(new { Updated = affected, minPrice, newPrice });
    }

    private static async ValueTask<IResult> DeleteOrderParentOnlyAsync(
        int id,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.DeleteGraphAsync<Order>(
            id,
            options =>
            {
                options.IncludeChildren = false;
                options.DeleteMode = ForgeDeleteMode.HardDelete;
            },
            ct);

        return Results.Ok(new { Deleted = affected, Id = id, Mode = "Parent only hard delete" });
    }

    private static async ValueTask<IResult> DeleteOrderGraphHardAsync(
        int id,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.DeleteGraphAsync<Order>(
            id,
            options =>
            {
                options.IncludeChildren = true;
                options.DeleteMode = ForgeDeleteMode.HardDelete;
            },
            ct);

        return Results.Ok(new { Deleted = affected, Id = id, Mode = "Graph hard delete" });
    }

    private static async ValueTask<IResult> DeleteOrderGraphHardByEntityAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.DeleteGraphAsync(
            order,
            options =>
            {
                options.IncludeChildren = true;
                options.DeleteMode = ForgeDeleteMode.HardDelete;
            },
            ct);

        return Results.Ok(new { Deleted = affected, order.Id, Mode = "Graph hard delete by entity" });
    }

    private static async ValueTask<IResult> DeleteOrderGraphSoftAsync(
        int id,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.DeleteGraphAsync<Order>(
            id,
            options =>
            {
                options.IncludeChildren = true;
                options.DeleteMode = ForgeDeleteMode.SoftDelete;
                options.SoftDeleteColumn = "IsDeleted";
            },
            ct);

        return Results.Ok(new { Deleted = affected, Id = id, Mode = "Graph soft delete" });
    }

    private static async ValueTask<IResult> DeleteOrderGraphSoftByEntityAsync(
        Order order,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.DeleteGraphAsync(
            order,
            options =>
            {
                options.IncludeChildren = true;
                options.DeleteMode = ForgeDeleteMode.SoftDelete;
                options.SoftDeleteColumn = "IsDeleted";
            },
            ct);

        return Results.Ok(new { Deleted = affected, order.Id, Mode = "Graph soft delete by entity" });
    }

    private static async ValueTask<IResult> DeleteProductsByConditionAsync(
        decimal maxPrice,
        ForgeDbContext db,
        CancellationToken ct)
    {
        var affected = await db.DeleteByConditionAsync<Product>(p => p.Price <= maxPrice, ct);
        return Results.Ok(new { Deleted = affected, maxPrice });
    }

    private static void NormalizeOrderTotals(CreateOrderRequest request)
    {
        var items = request.Items ?? [];
        request.GrandTotal = items.Sum(x => x.Quantity * x.UnitPrice);
        request.TotalAmount = request.GrandTotal;
    }

    private static void NormalizeOrderTotals(Order order)
    {
        order.Items ??= [];
        foreach (var item in order.Items)
        {
            item.LineTotal = item.Quantity * item.UnitPrice;
        }

        order.GrandTotal = order.Items.Sum(x => x.LineTotal);
        order.TotalAmount = order.GrandTotal;
    }
}
