# ForgeORM Studio Portal + Distributed Queries Update

This update adds the missing request/response contracts and working sample endpoints for the Studio portal, with special focus on `DistributedQueries` / federated query planning.

## Added / Fixed

- `FederatedPlanRequest`
- `FederatedDataSource`
- `FederatedSourceType`
- `FederatedExecutionMode`
- `FederatedPlanResult`
- `FederatedExecutionStep`
- `IForgeFederatedQueryPlanner.Plan(FederatedPlanRequest request)`
- Studio endpoint: `POST /studio/federated/plan`
- Studio portal panel: `Distributed Queries`
- Fixed `AppendEventRequest` → `IForgeEventStore.AppendAsync` by casting `StudioEvent` to `IForgeEvent`
- Fixed `StudioEvent` implementation of `IForgeEvent`
- Fixed `GenerateErpRequest` endpoint mapping
- Fixed `AuthorizeRequest` endpoint mapping
- Fixed `SyncRequest` endpoint mapping
- Enabled CORS for Studio React portal

## Distributed Query Example

```json
{
  "query": "Get customer sales summary",
  "sources": [
    { "name": "sales-sql", "type": 1, "database": "SalesDb" },
    { "name": "reporting-pg", "type": 2, "database": "ReportingDb" },
    { "name": "crm-api", "type": 9 }
  ],
  "executionMode": 2,
  "enableCaching": true,
  "enableTelemetry": true,
  "enableOptimization": true,
  "enableSecurityValidation": true
}
```

## Run Studio API

```bash
dotnet run --project src/ForgeORM.Studio.Api/ForgeORM.Studio.Api.csproj
```

## Run React Studio

```bash
cd studio/ForgeORM.Studio.Web
npm install
npm run dev
```

Set API base URL when needed:

```bash
VITE_FORGEORM_STUDIO_API=http://localhost:5000 npm run dev
```
