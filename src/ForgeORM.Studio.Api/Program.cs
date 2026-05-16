using ForgeORM.AI.Advanced;
using ForgeORM.Caching.Redis;
using ForgeORM.Security;
using ForgeORM.Telemetry;
using ForgeORM.VectorSearch;
using ForgeORM.Rag;
using ForgeORM.Workflow;
using ForgeORM.EventSourcing;
using ForgeORM.Realtime;
using ForgeORM.AI.Agents;
using ForgeORM.LowCode;
using ForgeORM.Cloud;
using ForgeORM.Identity;
using ForgeORM.Sync;
using ForgeORM.Marketplace;
using ForgeORM.DataVirtualization;
using ForgeORM.TimeTravel;
using ForgeORM.Observability.AI;
using ForgeORM.AI.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddForgeMemoryQueryCaching();
builder.Services.AddForgeTelemetry();
builder.Services.AddForgeSecurity();
builder.Services.AddForgeInMemoryVectorSearch();
builder.Services.AddForgeAdvancedAi();
builder.Services.AddForgeRag();
builder.Services.AddForgeWorkflow();
builder.Services.AddForgeEventSourcing();
builder.Services.AddForgeRealtime();
builder.Services.AddForgeAiAgents();
builder.Services.AddForgeLowCode();
builder.Services.AddForgeCloudDeployment();
builder.Services.AddForgeIdentityPolicies();
builder.Services.AddForgeOfflineSync();
builder.Services.AddForgeMarketplace();
builder.Services.AddForgeDataVirtualization();
builder.Services.AddForgeTimeTravel();
builder.Services.AddForgeAiObservability();
builder.Services.AddForgeAiMemory();

var app = builder.Build();
app.UseCors();
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



app.MapPost("/studio/rag/ingest", async (ForgeRagDocument document, IForgeRagEngine rag) => Results.Ok(await rag.IngestAsync(document)));
app.MapPost("/studio/rag/context", async (RagQuestionRequest request, IForgeRagEngine rag) => Results.Ok(await rag.BuildContextAsync(request.Question, request.TopK)));

app.MapPost("/studio/workflows/run", async (ForgeWorkflowDefinition workflow, IForgeWorkflowEngine engine) => Results.Ok(await engine.RunAsync(workflow)));
app.MapPost("/studio/workflows/designer", (ForgeWorkflowDefinition workflow, IForgeWorkflowEngine engine) => Results.Ok(engine.ToDesignerModel(workflow)));

app.MapPost("/studio/events/append",
    async (
        AppendEventRequest request,
        IForgeEventStore store) =>
    {
        if (string.IsNullOrWhiteSpace(request.StreamId))
            return Results.BadRequest("StreamId is required.");

        if (string.IsNullOrWhiteSpace(request.EventType))
            return Results.BadRequest("EventType is required.");

        var studioEvent = new StudioEvent
        {
            Id = Guid.NewGuid(),
            Type = request.EventType,
            Title = request.EventType,
            Description = $"Event appended to stream {request.StreamId}",
            Payload = request.Data,
            TenantId = request.TenantId,
            UserId = request.UserId,
            Severity = "Information",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = request.Metadata
        };

        await store.AppendAsync(
            request.StreamId,
            new IForgeEvent[] { studioEvent });

        return Results.Ok(new
        {
            appended = true,
            request.StreamId,
            eventId = studioEvent.Id
        });
    });
app.MapGet("/studio/events/{streamId}", async (string streamId, IForgeEventStore store) => Results.Ok(await store.ReadStreamAsync(streamId)));

app.MapPost("/studio/realtime/publish", async (ForgeRealtimeEvent evt, IForgeRealtimeHub hub) =>
{
    await hub.PublishAsync(evt);
    return Results.Ok(new { published = true, evt.Topic });
});

app.MapPost("/studio/agents/run", async (ForgeAgentTask task, ForgeAgentRunner runner) => Results.Ok(await runner.RunAllAsync(task)));
app.MapPost("/studio/lowcode/erp", (GenerateErpRequest request, IForgeLowCodeEngine lowCode) =>
{
    var domain = string.IsNullOrWhiteSpace(request.CompanyName)
        ? request.Industry
        : $"{request.CompanyName} {request.Industry}";

    return Results.Ok(lowCode.GenerateErp(domain, request.Modules));
});
app.MapPost("/studio/cloud/deployment", (CloudDeploymentRequest request, IForgeDeploymentGenerator generator) => Results.Ok(generator.Generate(request)));
app.MapPost("/studio/identity/authorize", (ForgeORM.Identity.AuthorizeRequest request, IForgePolicyEngine policies) =>
{
    var decision = policies.Authorize(request.ToPrincipal(), request.ToRequirement());
    return Results.Ok(new AuthorizationResult
    {
        IsAuthorized = decision.Allowed,
        Policy = $"{request.Resource}:{request.Action}",
        Reason = decision.Reason
    });
});
app.MapPost("/studio/sync", async (SyncRequest request, IForgeSyncEngine sync) => Results.Ok(await sync.SynchronizeAsync(request)));
app.MapPost("/studio/marketplace/publish", (ForgeMarketplaceItem item, IForgeMarketplaceCatalog catalog) => { catalog.Publish(item); return Results.Ok(item); });
app.MapGet("/studio/marketplace", (string? q, string? category, IForgeMarketplaceCatalog catalog) => Results.Ok(catalog.Search(q, category)));
app.MapPost("/studio/federated/plan", (FederatedPlanRequest request, IForgeFederatedQueryPlanner planner) => Results.Ok(planner.Plan(request)));
app.MapPost("/studio/time-travel/sql", (TimeTravelQuery request, IForgeTimeTravelSqlBuilder builder) => Results.Ok(builder.BuildSql(request)));
app.MapGet("/studio/observability/ai", (IForgeTelemetry telemetry, IForgeAiObservabilityAnalyzer analyzer) => Results.Ok(analyzer.Analyze(telemetry.Snapshot())));
app.MapPost("/studio/memory/remember", async (ForgeMemoryEntry entry, IForgeAiMemoryStore memory) => { await memory.RememberAsync(entry); return Results.Ok(entry); });
app.MapGet("/studio/memory/{scope}", async (string scope, string? q, IForgeAiMemoryStore memory) => Results.Ok(await memory.RecallAsync(scope, q)));

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
    /// <summary>
    /// Initializes or executes the ExtractNodes operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Initializes or executes the ExtractEdges operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The operation result.</returns>
    public static IReadOnlyList<object> ExtractEdges(string sql)
    {
        var nodes = ExtractNodes(sql).Select(x => x.ToString()).ToList();
        return nodes.Skip(1).Select((n, i) => new { from = nodes[0], to = n, type = "join" }).ToList<object>();
    }
}
