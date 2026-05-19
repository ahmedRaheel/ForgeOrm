# ForgeORM concurrency, locking and large-data update

Applied to the uploaded ForgeOrm(14).zip.

## Fixed compile blockers
- Disabled duplicate `src/ForgeORM.Core/QueryBuilder/ForgeQueryProfiling.cs` because `ForgeQueryBuilder.cs` already contains the renamed QueryBuilder-safe profiling classes:
  - `ForgeQueryBuilderProfileEntry`
  - `ForgeQueryBuilderAnalysis`
  - `ForgeQueryBuilderProfiler`
  - `ForgeQueryBuilderIndexSuggestionEngine`
- Fixed invalid namespace typo:
  - `ForgeORM.Abstractions. ForgeQueryAnalysis`
  - corrected to `ForgeORM.Abstractions.ForgeQueryAnalysis`
- Added `ForgeDb.Analysis` facade:
  - `db.Analysis.Analyze(sql)`
  - `db.Analysis.Analyze(db.Query<T>()...)`
  - `db.Analysis.Profiles()`
  - `db.Analysis.ClearProfiles()`

## Added enterprise concurrency / locking APIs
- `NoLock()`
- `UpdateLock()`
- `ReadPast()`
- `RowLock()`
- `SnapshotRead()`
- `WithReadConsistency(...)`

## Added high-volume query APIs
- `QueryThrottledAsync(...)`
- `QueryKeysetPageAsync(...)`
- `ProcessKeysetBatchesAsync(...)`
- `ForgeEnterpriseQueryMonitor`

## Added sample endpoints
- `/enterprise-concurrency/products/nolock`
- `/enterprise-concurrency/products/snapshot-read`
- `/enterprise-concurrency/products/readpast-queue`
- `/enterprise-concurrency/products/update-lock`
- `/enterprise-concurrency/products/read-consistency/{mode}`
- `/enterprise-concurrency/products/throttled`
- `/enterprise-concurrency/products/keyset`
- `/enterprise-concurrency/products/process-batches`
- `/enterprise-concurrency/monitor/snapshot`
- `/enterprise-concurrency/monitor/clear`
