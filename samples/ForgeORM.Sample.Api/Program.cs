using ForgeORM.AspNetCore;
using ForgeORM.AI.Advanced;
using ForgeORM.Caching.Redis;
using ForgeORM.Security;
using ForgeORM.Telemetry;
using ForgeORM.VectorSearch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddForgeOrm(options => options.UseSqlServer(connectionString));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddForgeMemoryQueryCaching();
builder.Services.AddForgeTelemetry();
builder.Services.AddForgeSecurity();
builder.Services.AddForgeInMemoryVectorSearch();
builder.Services.AddForgeAdvancedAi();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapAiVectorEndpoints();
app.MapAnalyticsEndpoints();
app.MapArtifactEndpoints();
app.MapBulkTransactionEndpoints();
app.MapCacheSecurityTelemetryEndpoints();
app.MapDataFrameImportEndpoints();
app.MapGraphPersistenceEndpoints();
app.MapQueryBuilderEndpoints();
app.MapRawSqlEndpoints();
app.MapSavedQueryEndpoints();
app.MapSearchEndpoints();
app.MapSplitQueryEndpoints();
app.MapStoredProcedureFunctionEndpoints();
app.MapUserFriendlyExampleEndpoints();
app.MapEnterpriseFeatureEndpoints();
app.MapEnterpriseConcurrencyEndpoints();


app.MapEnterpriseAdvancedFeaturePackEndpoints();

app.Run();
