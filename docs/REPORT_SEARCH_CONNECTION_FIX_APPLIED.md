# Report/Search/Connection Fix Applied

Fixed:
- Dynamic materialization moved directly onto ForgeDb so extensions do not call private CreateConnection().
- Added package-level dynamic materializers:
  - QueryDictionaryAsync
  - QueryJsonProjectionAsync
  - QueryJsonAsync
  - QueryDataFrameAsync
  - QueryCsvAsync
- Moved/added ForgeSearch package implementation under:
  - src/ForgeORM.Querying/Search/ForgeSearch.cs
- Sample search endpoints now only use package APIs.
- ForgeReport endpoints are under:
  - samples/ForgeORM.Sample.Api/Endpoints/ForgeReportEndpoints.cs
- Program.cs wired:
  - app.MapForgeReportEndpoints()
  - app.MapSearchEndpoints()

Moved loose report endpoint files:
samples/ForgeORM.Sample.Api/ReportingEndpoints.cs -> samples/ForgeORM.Sample.Api/Endpoints/ReportingEndpoints.cs

Removed duplicate sample search implementations:
samples/ForgeORM.Sample.Api/Search/ForgeSearch.cs
