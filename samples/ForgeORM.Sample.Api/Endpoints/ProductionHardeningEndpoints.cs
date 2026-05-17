using ForgeORM.Core.AI;
using ForgeORM.Core.Compiled;
using ForgeORM.Core.DataFrame;
using ForgeORM.Core.Providers;
using ForgeORM.Core.Reliability;
using ForgeORM.Core.Security;

public static class ProductionHardeningEndpoints
{
    public static IEndpointRouteBuilder MapProductionHardeningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/production-hardening")
            .WithTags("102 Production Hardening / AI / DataFrame");

        group.MapGet("/providers/capabilities/{provider}", (string provider) =>
        {
            var parsed = Enum.TryParse<ForgePhysicalProvider>(provider, true, out var p)
                ? p
                : ForgePhysicalProvider.SqlServer;

            var dialect = ForgeProviderDialectRegistry.Create(parsed);

            return Results.Ok(new
            {
                provider = parsed.ToString(),
                dialect.Capabilities,
                sampleUpsert = dialect.RenderUpsert("Products", ["Id"], ["Id", "Name", "Price"]),
                samplePaging = dialect.RenderLimitOffset("SELECT * FROM Products ORDER BY Id", 10, 20)
            });
        });

        group.MapGet("/compiled/entity-plan/product", () =>
        {
            var plan = ForgeCompiledPlanCache.For<SampleProductForHardening>();

            return Results.Ok(new
            {
                plan.TableName,
                Properties = plan.Properties.Select(x => new { x.Name, Type = x.PropertyType.Name }),
                Key = plan.Key?.Name,
                plan.InsertSql,
                plan.SelectByIdSql,
                plan.UpdateSql,
                plan.DeleteSql
            });
        });

        group.MapGet("/compiled/graph-plan/order", () =>
        {
            var plan = ForgeCompiledGraphPlanCache.For<SampleOrderForHardening>(typeof(SampleOrderItemForHardening));

            return Results.Ok(plan);
        });

        group.MapGet("/reliability/retry-success", async (CancellationToken ct) =>
        {
            var result = await ForgeReliabilityExecutor.ExecuteAsync(
                _ => Task.FromResult("Success through retry pipeline"),
                new ForgeRetryPolicy(MaxRetries: 3),
                new ForgeCircuitBreaker(),
                ct);

            return Results.Ok(new { result });
        });

        group.MapPost("/security/validate-sql", (SqlValidationRequest request) =>
        {
            var result = ForgeSqlSecurityHardener.Validate(request.Sql);

            return Results.Ok(result);
        });

        group.MapGet("/security/mask", (string value) =>
            Results.Ok(new
            {
                original = value,
                masked = ForgeSqlSecurityHardener.MaskPii(value)
            }));

        group.MapPost("/security/tenant-isolation", (SqlValidationRequest request) =>
            Results.Ok(ForgeTenantIsolationGuard.ValidateQuery(request.Sql)));

        group.MapPost("/ai/generate-sql", (ForgeAiSqlRequest request) =>
            Results.Ok(ForgeAiQueryAssistant.GenerateSql(request)));

        group.MapGet("/ai/schema-insight/order", () =>
            Results.Ok(ForgeAiQueryAssistant.AnalyzeSchema(
                "Order",
                ["Id", "CustomerId", "OrderNo", "GrandTotal", "CreatedAt"])));

        group.MapGet("/dataframe/cleaning", () =>
        {
            var frame = new ForgeEnterpriseDataFrame()
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 1, ["Revenue"] = 100m, ["Region"] = null })
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 1, ["Revenue"] = 100m, ["Region"] = null })
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 2, ["Revenue"] = 250m, ["Region"] = "PK" });

            frame.FillNull("Region", "Unknown")
                .DropDuplicates("CustomerId", "Revenue");

            return Results.Ok(new
            {
                frame.RowCount,
                frame.Rows,
                csv = frame.ToCsv()
            });
        });

        group.MapGet("/dataframe/join-rolling", () =>
        {
            var sales = new ForgeEnterpriseDataFrame()
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 1, ["Revenue"] = 100m })
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 2, ["Revenue"] = 200m })
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 3, ["Revenue"] = 300m });

            var customers = new ForgeEnterpriseDataFrame()
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 1, ["Name"] = "Alpha" })
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 2, ["Name"] = "Beta" })
                .AddRow(new Dictionary<string, object?> { ["CustomerId"] = 3, ["Name"] = "Gamma" });

            var joined = sales.Join(customers, "CustomerId", "CustomerId")
                .RollingAverage("Revenue", "RevenueRollingAverage", 2);

            return Results.Ok(new
            {
               joined 
            });
        });

        group.MapGet("/benchmark/how-to-run", () =>
            Results.Ok(new
            {
                command = "dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks",
                purpose = "Benchmark Forge compiled accessors/plans and extend comparisons against Dapper and EF Core."
            }));

        return app;
    }

    private sealed record SqlValidationRequest(string Sql);

    private sealed class SampleProductForHardening
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    private sealed class SampleOrderForHardening
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = "";
    }

    private sealed class SampleOrderItemForHardening
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
    }
}
