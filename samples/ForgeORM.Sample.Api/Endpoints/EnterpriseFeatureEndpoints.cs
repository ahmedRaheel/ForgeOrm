using ForgeORM.Core;
using ForgeORM.Core.Graph;
using ForgeORM.DataFrame;

public static class EnterpriseFeatureEndpoints
{
    public static IEndpointRouteBuilder MapEnterpriseFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/enterprise-features").WithTags("99 Enterprise Feature Coverage 1-10");

        group.MapGet("/01-complex/exists", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Query<Product>()
                .From("dbo.Products p")
                .WhereExists("SELECT 1 FROM dbo.OrderItems oi WHERE oi.ProductId = p.Id AND oi.Quantity > @Qty", new { Qty = 10 })
                .OrderByDescending(x => x.Id)
                .Take(50)
                .ToListAsync(ct);
            return Results.Ok(rows);
        });

        group.MapGet("/01-complex/case-json-fulltext", (ForgeDbContext db) =>
        {
            var debug = db.Query<Product>()
                .From("dbo.Products")
                .Select(x => x.Id, x => x.Name, x => x.Price)
                .CaseWhen("Price >= 1000", "'Premium'", "'Standard'", "PriceBand")
                .WhereJsonValue("MetadataJson", "$.color", "=", "black")
                .WhereFullText("Name", "enterprise")
                .ToDebugSql();
            return Results.Ok(debug);
        });

        group.MapGet("/02-performance/profile-analyze", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var query = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(50)
                .Profile("HighValueProducts")
                .Tag("Dashboard")
                .Comment("Used by enterprise sample")
                .CacheFor(TimeSpan.FromMinutes(5));

            var analysis = query.Analyze();
            var rows = await query.ToListAsync(ct);
            return Results.Ok(new { rows, analysis, profiles = ForgeQueryProfiler.Snapshot() });
        });

        group.MapGet("/02-performance/compiled", async (ForgeDbContext db, CancellationToken ct) =>
        {
            var rows = await db.CompiledQuery<Product>("CompiledHighValueProducts")
                .Configure(q => q.From("dbo.Products").Where(x => x.Price > 100).OrderByDescending(x => x.Id).Take(20))
                .ExecuteAsync(ct);
            return Results.Ok(rows);
        });

        group.MapGet("/02-performance/explain-validate", (ForgeDbContext db) =>
        {
            var query = db.Query<Product>().From("dbo.Products").Take(20);
            return Results.Ok(new { explain = query.Explain(), validation = query.Validate(), sql = query.ToSql() });
        });

        group.MapGet("/03-mapping/debug-projection", (ForgeDbContext db) =>
        {
            var sql = db.Query<Product>()
                .From("dbo.Products p")
                .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
                .SelectSql("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
                .ToSql();
            return Results.Ok(new { sql });
        });

        group.MapGet("/04-dataframe/advanced", async (ForgeDbContext db, CancellationToken ct) =>
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
                csvPreview = enriched.ExportCsvText().Split('\n').Take(5)
            });
        });

        group.MapGet("/05-schema/create-table-script", (ForgeDbContext db) =>
        {
            var diff = db.GenerateCreateTableScript<Product>();
            return Results.Ok(diff);
        });

        group.MapGet("/06-multitenant/query", (string tenantId, ForgeDbContext db) =>
        {
            var sql = db.Query<Order>()
                .From("dbo.Orders")
                .ForTenant("TenantId", tenantId)
                .OrderByDescending(x => x.Id)
                .Take(20)
                .ToDebugSql();
            return Results.Ok(sql);
        });

        group.MapGet("/07-security/validate-mask", (string sql, string email, ForgeDbContext db) =>
        {
            return Results.Ok(new
            {
                findings = db.ValidateSqlSafety(sql),
                maskedEmail = db.MaskEmail(email)
            });
        });

        group.MapPost("/08-bulk-sync/products", async (List<Product> rows, ForgeDbContext db, CancellationToken ct) =>
        {
            var result = await db.SyncAsync<Product, string>(rows, x => x.Code, options =>
            {
                options.InsertMissing = true;
                options.UpdateExisting = true;
                options.DeleteMissing = false;
                options.Strategy = ForgeBulkStrategy.Auto;
            }, ct);
            return Results.Ok(result);
        });

        group.MapPost("/09-outbox/order-created", async (Order order, ForgeDbContext db, CancellationToken ct) =>
        {
            var message = new ForgeOutboxMessage(Guid.NewGuid(), "OrderCreated", System.Text.Json.JsonSerializer.Serialize(order), DateTimeOffset.UtcNow);
            var affected = await db.SaveWithOutboxAsync(order, message, ct);
            return Results.Ok(new { affected, outboxId = message.Id });
        });

        group.MapGet("/10-devex/debug", (ForgeDbContext db) =>
        {
            var query = db.Query<Product>()
                .From("dbo.Products")
                .Where(x => x.Price > 100)
                .OrderByDescending(x => x.Id)
                .Take(10)
                .Tag("DeveloperExperience")
                .Comment("Debug SQL sample");
            var clone = query.Clone().Take(5);
            return Results.Ok(new { sql = query.ToDebugSql(), clone = clone.ToDebugSql(), validation = query.Validate() });
        });

        return app;
    }
}
