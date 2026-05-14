using ForgeORM.AI.Advanced;
using ForgeORM.Caching.Redis;
using ForgeORM.Security;
using ForgeORM.Telemetry;
using ForgeORM.VectorSearch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddForgeMemoryQueryCaching();
builder.Services.AddForgeTelemetry();
builder.Services.AddForgeSecurity();
builder.Services.AddForgeInMemoryVectorSearch();
builder.Services.AddForgeAdvancedAi();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "ForgeORM Studio API");

app.MapPost("/studio/query/visualize", (QueryVisualizeRequest request, IForgeSqlSecurityValidator security, IForgeAiOptimizer optimizer) =>
{
    var safety = security.Validate(request.Sql, allowDdl: false);
    var optimization = optimizer.Optimize(new ForgeAiOptimizationRequest(request.Sql, request.Provider));
    return Results.Ok(new
    {
        request.Provider,
        safety,
        optimization,
        nodes = QueryVisualizer.ExtractNodes(request.Sql),
        edges = QueryVisualizer.ExtractEdges(request.Sql)
    });
});

app.MapPost("/studio/api-test", async (ApiTestRequest request) =>
{
    using var client = new HttpClient();
    using var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);
    foreach (var h in request.Headers ?? new Dictionary<string,string>()) message.Headers.TryAddWithoutValidation(h.Key, h.Value);
    if (!string.IsNullOrWhiteSpace(request.Body)) message.Content = new StringContent(request.Body);
    var response = await client.SendAsync(message);
    return Results.Ok(new { status = (int)response.StatusCode, body = await response.Content.ReadAsStringAsync() });
});

app.MapGet("/studio/erd/sample", () => Results.Ok(new ErdDiagram(
    [new ErdEntity("Products", ["Id", "Name", "Price", "CategoryId"]), new ErdEntity("Categories", ["Id", "Name"])],
    [new ErdRelationship("Products", "Categories", "CategoryId", "Id", "many-to-one")])));

app.MapGet("/studio/monitoring", (IForgeTelemetry telemetry) => Results.Ok(telemetry.Snapshot()));

app.MapPost("/studio/vector/upsert", async (ForgeVectorDocument doc, IForgeVectorStore store) =>
{
    await store.UpsertAsync(doc);
    return Results.Ok(new { upserted = doc.Id });
});

app.MapPost("/studio/vector/search", async (VectorSearchRequest request, IForgeVectorStore store) =>
    Results.Ok(await store.SearchAsync(request.Vector, request.TopK)));

app.MapPost("/studio/ai/optimize", (ForgeAiOptimizationRequest request, IForgeAiOptimizer optimizer) => Results.Ok(optimizer.Optimize(request)));
app.MapPost("/studio/ai/diagnose", (AiDiagnoseRequest request, IForgeAiDiagnostics diagnostics) => Results.Ok(diagnostics.Diagnose(request.Sql, TimeSpan.FromMilliseconds(request.ElapsedMs), request.RowCount)));
app.MapPost("/studio/ai/generate-crud", (GenerateCrudRequest request, IForgeAiCodeGenerator generator) => Results.Ok(generator.GenerateMinimalApiCrud(request.EntityName, request.RoutePrefix)));
app.MapPost("/studio/ai/migration/add-column", (AddColumnMigrationRequest request, IForgeAiMigrationPlanner planner) => Results.Ok(planner.PlanAddColumn(request.Table, request.Column, request.SqlType, request.Nullable)));

app.MapGet("/studio/saas/tenants", () => Results.Ok(new[]
{
    new { id = "default", name = "Default Tenant", status = "Active" },
    new { id = "enterprise", name = "Enterprise Tenant", status = "Active" }
}));

app.Run();

public sealed record QueryVisualizeRequest(string Sql, string Provider = "SqlServer");
public sealed record ApiTestRequest(string Method, string Url, Dictionary<string,string>? Headers, string? Body);
public sealed record VectorSearchRequest(float[] Vector, int TopK = 5);
public sealed record AiDiagnoseRequest(string Sql, double ElapsedMs, int RowCount);
public sealed record GenerateCrudRequest(string EntityName, string RoutePrefix);
public sealed record AddColumnMigrationRequest(string Table, string Column, string SqlType, bool Nullable = true);
public sealed record ErdEntity(string Name, IReadOnlyList<string> Columns);
public sealed record ErdRelationship(string From, string To, string FromColumn, string ToColumn, string Kind);
public sealed record ErdDiagram(IReadOnlyList<ErdEntity> Entities, IReadOnlyList<ErdRelationship> Relationships);

public static class QueryVisualizer
{
    public static IReadOnlyList<object> ExtractNodes(string sql)
    {
        var tokens = sql.Split([' ', '\r', '\n', '\t', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var nodes = new List<object>();
        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Equals("FROM", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Length) nodes.Add(new { id = tokens[i + 1], type = "table" });
            if (tokens[i].Equals("JOIN", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Length) nodes.Add(new { id = tokens[i + 1], type = "join" });
        }
        return nodes;
    }

    public static IReadOnlyList<object> ExtractEdges(string sql)
    {
        var nodes = ExtractNodes(sql).Select(x => x.ToString()).ToList();
        return nodes.Skip(1).Select((n, i) => new { from = nodes[0], to = n, type = "join" }).ToList<object>();
    }
}
