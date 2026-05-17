
using ForgeORM.Abstractions;
using ForgeORM.AspNetCore;
using ForgeORM.Core;
using ForgeORM.AI.Advanced;
using ForgeORM.Analytics;
using ForgeORM.Analytics.Reporting;
using ForgeORM.DataFrame;
using ForgeORM.Core.Search;
using ForgeORM.Core.SplitQuery;
using ForgeORM.Core.SavedQueries;
using ForgeORM.QueryAst;
using ForgeORM.QueryAst.Artifacts;
using ForgeORM.SchemaOps;
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

app.MapNextGenFeaturePackEndpoints();

app.MapNextGenDetailedSampleEndpoints();

app.MapProductionHardeningEndpoints();

app.MapUserFriendlyReportingMaterializerEndpoints();

app.MapThreeEntryStylesEndpoints();

app.Run();
