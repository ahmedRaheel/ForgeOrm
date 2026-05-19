using ForgeORM.AI.Advanced;
using ForgeORM.Core;
using ForgeORM.VectorSearch;

public static class AiVectorEndpoints
{
    public static IEndpointRouteBuilder MapAiVectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ai-vector").WithTags("10 AI / Vector Search");

        group.MapPost("/optimize", (string sql, IForgeAiOptimizer optimizer) =>
            Results.Ok(optimizer.Optimize(new ForgeAiOptimizationRequest(sql))));

        group.MapPost("/diagnose", (string sql, double elapsedMs, int rowCount, IForgeAiDiagnostics diagnostics) =>
            Results.Ok(diagnostics.Diagnose(sql, TimeSpan.FromMilliseconds(elapsedMs), rowCount)));

        group.MapPost("/generate-crud", (string entityName, string routePrefix, IForgeAiCodeGenerator generator) =>
            Results.Ok(generator.GenerateMinimalApiCrud(entityName, routePrefix)));

        group.MapPost("/migration/add-column", (string table, string column, string sqlType, bool nullable, IForgeAiMigrationPlanner planner) =>
            Results.Ok(planner.PlanAddColumn(table, column, sqlType, nullable)));

        group.MapPost("/vector/upsert", async (ForgeVectorDocument document, IForgeVectorStore store) =>
        {
            await store.UpsertAsync(document);
            return Results.Ok(new { document.Id, Upserted = true });
        });

        group.MapPost("/vector/search", async (float[] vector, int topK, IForgeVectorStore store) =>
            Results.Ok(await store.SearchAsync(vector, topK)));

        return app;
    }
}
