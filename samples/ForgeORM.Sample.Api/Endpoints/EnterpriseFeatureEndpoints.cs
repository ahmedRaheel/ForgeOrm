using ForgeORM.Analytics.Reporting;
using ForgeORM.Core;
using ForgeORM.Core.Graph;
using ForgeORM.DataFrame;
using System.Text.Json;

public static class EnterpriseFeatureEndpoints
{
    public static IEndpointRouteBuilder MapEnterpriseFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        MapComplexQueryPower(app);
        MapQueryPerformanceEngine(app);
        MapAdvancedMapping(app);
        MapEnterpriseDataFrame(app);
        MapMigrationSchemaManagement(app);
        MapMultiTenant(app);
        MapSecurity(app);
        MapBulkSync(app);
        MapEventOutbox(app);
        MapDeveloperExperience(app);

        return app;
    }

    private static void MapComplexQueryPower(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/01-complex-query-power")
            .WithTags("Enterprise 01 - Complex Query Power");

        group.MapGet("/exists", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products p")
                .WhereExists("SELECT 1 FROM dbo.OrderItems oi WHERE oi.ProductId = p.Id AND oi.Quantity > @Qty", new { Qty = 10 })
                .OrderByDescending(x => x.Id)
                .Take(50)
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/not-exists", (ForgeDbContext db) =>
        {
            var sql = db.Query<Product>()
                .From("dbo.Products p")
                .WhereNotExists("SELECT 1 FROM dbo.OrderItems oi WHERE oi.ProductId = p.Id")
                .OrderByDescending(x => x.Id)
                .Take(50)
                .ToDebugSql();

            return Results.Ok(sql);
        });

        group.MapGet("/in-subquery", (ForgeDbContext db) =>
        {
            var sql = db.Query<Product>()
                .From("dbo.Products")
                .WhereInSubQuery("CategoryId", "SELECT Id FROM dbo.Categories WHERE Name LIKE @Name", new { Name = "%Hardware%" })
                .OrderByDescending(x => x.Id)
                .Take(20)
                .ToDebugSql();

            return Results.Ok(sql);
        });

        group.MapGet("/case-json-fulltext", (ForgeDbContext db) =>
        {
            var sql = db.Query<Product>()
                .From("dbo.Products")
                .Select(x => x.Id, x => x.Name, x => x.Price)
                .CaseWhen("Price >= 1000", "'Premium'", "'Standard'", "PriceBand")
                .WhereJsonValue("MetadataJson", "$.color", "=", "black")
                .WhereFullText("Name", "enterprise")
                .ToDebugSql();

            return Results.Ok(sql);
        });

        group.MapGet("/lateral-cross-apply", (ForgeDbContext db) =>
        {
            var sql = db.Query<Product>()
                .From("dbo.Products p")
                .Lateral("SELECT TOP 1 oi.Quantity FROM dbo.OrderItems oi WHERE oi.ProductId = p.Id ORDER BY oi.Id DESC", "latestItem")
                .SelectSql("p.Id", "p.Name", "latestItem.Quantity AS LatestQuantity")
                .Take(25)
                .ToDebugSql();

            return Results.Ok(sql);
        });

        group.MapGet("/temporal-as-of", (DateTimeOffset asOf, ForgeDbContext db) =>
        {
            var sql = db.Query<Order>()
                .From("dbo.Orders")
                .ForSystemTimeAsOf(asOf)
                .Where(x => x.TotalAmount > 0)
                .Take(20)
                .ToDebugSql();

            return Results.Ok(sql);
        });

        group.MapGet("/recursive-cte-debug", (ForgeDbContext db) =>
        {
            var sql = db.Query<Category>()
                .From("dbo.Categories")
                .WithRecursiveCte(
                    "CategoryTree",
                    "SELECT Id, Name, ParentId, 0 AS Level FROM dbo.Categories WHERE ParentId IS NULL",
                    "SELECT c.Id, c.Name, c.ParentId, ct.Level + 1 FROM dbo.Categories c INNER JOIN CategoryTree ct ON c.ParentId = ct.Id")
                .ToDebugSql();

            return Results.Ok(sql);
        });
    }

    private static void MapQueryPerformanceEngine(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/02-query-performance")
            .WithTags("Enterprise 02 - Query Performance Engine");

        group.MapGet("/profiled-products", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(50)
                .Profile("HighValueProducts")
                .Tag("Dashboard")
                .Comment("Profiled query sample")
                .ToListAsync(ct);

            return Results.Ok(new { rows, profiles = ForgeQueryProfiler.Snapshot() });
        });

        group.MapGet("/compiled-products", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.CompiledQuery<Product>("CompiledHighValueProducts")
                .Configure(q => q.From("dbo.Products").Where(x => x.Price > 100).OrderByDescending(x => x.Id).Take(20))
                .ExecuteAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/cached-products", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .CacheFor(TimeSpan.FromMinutes(5))
                .Take(20)
                .ToListAsync(ct);

            return Results.Ok(rows);
        });

        group.MapGet("/stream-products", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = new List<Product>();
            await foreach (var row in db.Query<Product>()
                .From("dbo.Products")
                .OrderByDescending(x => x.Id)
                .Take(10)
                .StreamAsync(ct))
            {
                rows.Add(row);
            }

            return Results.Ok(rows);
        });

        group.MapGet("/analyze-index", (ForgeDbContext db) =>
        {
            var analysis = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.CategoryId == 10)
                .OrderByDescending(x => x.Id)
                .Analyze();

            return Results.Ok(analysis);
        });

        group.MapGet("/explain-validate", (ForgeDbContext db) =>
        {
            var query = db.Query<Product>()
                .From("dbo.Products")
                .Take(20);

            return Results.Ok(new
            {
                explain = query.Explain(),
                validation = query.Validate(),
                sql = query.ToSql()
            });
        });
    }

    private static void MapAdvancedMapping(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/03-advanced-mapping")
            .WithTags("Enterprise 03 - Advanced Mapping");

        group.MapPost("/dto-to-entity", (ProductCreateRequest request) =>
        {
            var entity = request.MapTo<Product>();
            return Results.Ok(entity);
        });

        group.MapPost("/entity-to-dictionary", (Product product) =>
        {
            var dictionary = product.ToDictionaryProjection();
            return Results.Ok(dictionary);
        });

        group.MapPost("/dictionary-to-entity", (Dictionary<string, object?> values) =>
        {
            var product = values.FromDictionaryProjection<Product>();
            return Results.Ok(product);
        });

        group.MapPost("/json-to-dto", (string json) =>
        {
            var request = json.FromJsonProjection<CreateOrderRequest>();
            return Results.Ok(request);
        });

        group.MapPost("/enum-storage", (Order order) =>
        {
            return Results.Ok(new
            {
                enumValue = order.Status,
                asString = order.Status.ToString(),
                asNumber = (int)order.Status
            });
        });

        group.MapGet("/projection-sql", (ForgeDbContext db) =>
        {
            var sql = db.Query<Product>()
                .From("dbo.Products p")
                .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
                .SelectSql("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
                .Take(25)
                .ToDebugSql();

            return Results.Ok(sql);
        });
    }

    private static void MapEnterpriseDataFrame(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/04-dataframe")
            .WithTags("Enterprise 04 - DataFrame Features");

        group.MapGet("/clean-normalize-outliers", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var frame = await db.Frame<Order>()
                .FromSql("SELECT CustomerId, GrandTotal, TotalAmount FROM dbo.Orders")
                .ToFrameAsync(ct);

            var enriched = frame
                .FillNull(0)
                .DropDuplicates("CustomerId", "GrandTotal")
                .NormalizeColumn("GrandTotal", "NormalizedGrandTotal")
                .MovingAverage("GrandTotal", 3, "MovingAverage3")
                .DetectOutliers("GrandTotal");

            return Results.Ok(new
            {
                rows = enriched.Rows,
                correlation = enriched.Correlation("GrandTotal", "TotalAmount"),
                csvPreview = enriched.ExportCsvText().Split('\n').Take(5),
                htmlPreview = enriched.ExportHtmlTable()
            });
        });

        group.MapPost("/join-frames", () =>
        {
            var left = ForgeDataFrame.FromJsonText("""
[
  { "CustomerId": 1, "Revenue": 1000 },
  { "CustomerId": 2, "Revenue": 2000 }
]
""");
            var right = ForgeDataFrame.FromJsonText("""
[
  { "CustomerId": 1, "CustomerName": "Alpha" },
  { "CustomerId": 2, "CustomerName": "Beta" }
]
""");

            var joined = left.Join(right, "CustomerId");
            return Results.Ok(joined.Rows);
        });

        group.MapGet("/report-export-csv", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var csv = await db.Report<Order>("MonthlySalesCsv")
                .From("dbo.Orders")
                .Dimension("Status", "Status")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .Measure(ForgeReportMeasure.Count("*", "Orders"))
                .ExportCsvAsync(ct);

            return Results.File(csv, "text/csv", "monthly-sales.csv");
        });

        group.MapGet("/report-export-excel", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var excel = await db.Report<Order>("MonthlySalesExcel")
                .From("dbo.Orders")
                .Dimension("Status", "Status")
                .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
                .Measure(ForgeReportMeasure.Count("*", "Orders"))
                .ExportExcelAsync("Sales", ct);

            return Results.File(excel, "application/vnd.ms-excel", "monthly-sales.xls");
        });
    }

    private static void MapMigrationSchemaManagement(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/05-schema-migrations")
            .WithTags("Enterprise 05 - Migration / Schema Management");

        group.MapGet("/create-table/product", (ForgeDbContext db) =>
        {
            var diff = db.GenerateCreateTableScript<Product>();
            return Results.Ok(diff);
        });

        group.MapGet("/create-table/order", (ForgeDbContext db) =>
        {
            var diff = db.GenerateCreateTableScript<Order>();
            return Results.Ok(diff);
        });

        group.MapPost("/apply-product-table-script", async (bool execute, ForgeDbContext db, CancellationToken ct) =>
        {
            var diff = db.GenerateCreateTableScript<Product>();
            if (!execute)
            {
                return Results.Ok(new { dryRun = true, diff });
            }

            var affected = await db.ApplyMigrationAsync(diff, ct);
            return Results.Ok(new { applied = true, affected, diff });
        });
    }

    private static void MapMultiTenant(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/06-multitenant")
            .WithTags("Enterprise 06 - Multi-Tenant Support");

        group.MapGet("/tenant-query-debug", (string tenantId, ForgeDbContext db) =>
        {
            var sql = db.Query<Order>()
                .From("dbo.Orders")
                .ForTenant("TenantId", tenantId)
                .OrderByDescending(x => x.Id)
                .Take(20)
                .ToDebugSql();

            return Results.Ok(sql);
        });

        group.MapGet("/tenant-cache-key", (string tenantId, string queryName) =>
        {
            var key = $"tenant:{tenantId}:query:{queryName}";
            return Results.Ok(new { tenantId, queryName, cacheKey = key });
        });

        group.MapPost("/tenant-outbox", async (string tenantId, Order order, ForgeDbContext db, CancellationToken ct) =>
        {
            var message = new ForgeOutboxMessage(
                Guid.NewGuid(),
                "TenantOrderCreated",
                JsonSerializer.Serialize(order),
                DateTimeOffset.UtcNow,
                tenantId);

            var affected = await db.SaveWithOutboxAsync(order, message, ct);
            return Results.Ok(new { affected, tenantId, outboxId = message.Id });
        });
    }

    private static void MapSecurity(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/07-security")
            .WithTags("Enterprise 07 - Security");

        group.MapPost("/validate-sql", (string sql, ForgeDbContext db) =>
        {
            return Results.Ok(db.ValidateSqlSafety(sql));
        });

        group.MapGet("/mask-email", (string email, ForgeDbContext db) =>
        {
            return Results.Ok(new { original = email, masked = db.MaskEmail(email) });
        });

        group.MapGet("/safe-query-validation", (ForgeDbContext db) =>
        {
            var validation = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 10)
                .OrderByDescending(x => x.Id)
                .Take(10)
                .Validate();

            return Results.Ok(validation);
        });
    }

    private static void MapBulkSync(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/08-bulk-sync")
            .WithTags("Enterprise 08 - Bulk Sync");

        group.MapPost("/products-auto", async (List<Product> rows, ForgeDbContext db, CancellationToken ct) =>
        {
            var result = await db.SyncAsync<Product, string>(rows, x => x.Code, options =>
            {
                options.InsertMissing = true;
                options.UpdateExisting = true;
                options.DeleteMissing = false;
                options.Strategy = ForgeBulkStrategy.Auto;
                options.BatchSize = 1000;
            }, ct);

            return Results.Ok(result);
        });

        group.MapPost("/products-insert-only", async (List<Product> rows, ForgeDbContext db, CancellationToken ct) =>
        {
            var result = await db.SyncAsync<Product, string>(rows, x => x.Code, options =>
            {
                options.InsertMissing = true;
                options.UpdateExisting = false;
                options.DeleteMissing = false;
                options.Strategy = ForgeBulkStrategy.RowByRow;
            }, ct);

            return Results.Ok(result);
        });

        group.MapPost("/products-merge-strategy-debug", (List<Product> rows) =>
        {
            return Results.Ok(new
            {
                inputRows = rows.Count,
                sqlServer = "TVP/temp table + MERGE",
                postgreSql = "COPY + ON CONFLICT",
                mySql = "temp table + ON DUPLICATE KEY UPDATE",
                oracle = "array binding + MERGE"
            });
        });
    }

    private static void MapEventOutbox(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/09-outbox")
            .WithTags("Enterprise 09 - Event / Outbox Support");

        group.MapPost("/save-message", async (string type, string payload, ForgeDbContext db, CancellationToken ct) =>
        {
            var message = new ForgeOutboxMessage(Guid.NewGuid(), type, payload, DateTimeOffset.UtcNow);
            var affected = await db.SaveOutboxAsync(message, ct);
            return Results.Ok(new { affected, message.Id, message.Type });
        });

        group.MapPost("/save-order-with-outbox", async (Order order, ForgeDbContext db, CancellationToken ct) =>
        {
            var message = new ForgeOutboxMessage(
                Guid.NewGuid(),
                "OrderCreated",
                JsonSerializer.Serialize(order),
                DateTimeOffset.UtcNow);

            var affected = await db.SaveWithOutboxAsync(order, message, ct);
            return Results.Ok(new { affected, outboxId = message.Id, eventType = message.Type });
        });

        group.MapGet("/dispatcher-contract", () =>
        {
            return Results.Ok(new
            {
                table = "ForgeOutbox",
                statuses = new[] { "Pending", "Processing", "Dispatched", "Failed" },
                recommendedPattern = "SELECT pending rows with lock, publish, mark dispatched, retry failures with exponential backoff."
            });
        });
    }

    private static void MapDeveloperExperience(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise/10-developer-experience")
            .WithTags("Enterprise 10 - Developer Experience APIs");

        group.MapGet("/to-sql", (ForgeDbContext db) =>
        {
            var sql = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(10)
                .ToSql();

            return Results.Ok(new { sql });
        });

        group.MapGet("/to-debug-sql", (ForgeDbContext db) =>
        {
            var debug = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(10)
                .Tag("DeveloperExperience")
                .Comment("Debug SQL sample")
                .ToDebugSql();

            return Results.Ok(debug);
        });

        group.MapGet("/clone-query", (ForgeDbContext db) =>
        {
            var query = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(10);

            var clone = query.Clone().Take(5);
            return Results.Ok(new { original = query.ToDebugSql(), clone = clone.ToDebugSql() });
        });

        group.MapGet("/tag-comment-timeout", (ForgeDbContext db) =>
        {
            var debug = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .Timeout(30)
                .Tag("ProductsScreen")
                .Comment("Used by admin product list")
                .ToDebugSql();

            return Results.Ok(debug);
        });
    }
}
