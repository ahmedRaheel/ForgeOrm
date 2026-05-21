# ForgeORM Enterprise Architecture Upgrade Applied

This patch strengthens ForgeORM as an enterprise-grade, high-performance ORM while preserving the current public API surface.

## Applied Runtime Enhancements

### 1. Central Enterprise Runtime Layer
Added `ForgeEnterpriseRuntime`, a low-overhead runtime layer for:

- command interception
- audit hooks
- slow-query detection
- normalized query metrics
- failure telemetry
- OpenTelemetry bridge points
- CI performance guard support

The runtime is opt-in. When disabled, the hot path pays only a single branch check.

### 2. Command Interceptor Pipeline
Added `IForgeCommandInterceptor` with:

- `CommandExecutingAsync`
- `CommandExecutedAsync`
- `CommandFailedAsync`

This gives ForgeORM enterprise hooks similar to EF interceptors while keeping the Dapper-like raw execution path lightweight.

### 3. Query Metrics and Slow Query Guard
Added aggregated query metrics per normalized query fingerprint:

- count
- failures
- average elapsed
- max elapsed
- last elapsed
- provider
- SQL fingerprint

Added `ForgeSlowQueryGuardInterceptor` for CI/performance regression gates.

### 4. Performance Pipeline Integration
`ForgePerformancePipeline` now wraps query, scalar, execute, single, first and stream paths with optional enterprise hooks.

Execution flow:

```text
Public API
  ↓
ForgePerformancePipeline
  ↓
Compiled query plan cache
  ↓
Cached parameter binder
  ↓
DbCommand execution
  ↓
Source-generated / provider-specific / MSIL materializer
  ↓
Enterprise metrics/interceptors when enabled
```

### 5. ValueTask Public Hot Path APIs
Added non-breaking ValueTask APIs to `ForgeDb`:

- `QueryValueAsync<T>`
- `QueryValueAsync<T,TParameters>`
- `ExecuteScalarValueAsync<T>`
- `StreamValueAsync<T>`
- `WarmupQuery<T>`

These reduce async wrapper allocation in hot endpoints without removing existing `Task<IReadOnlyList<T>>` methods.

### 6. Query Plan Warmup
Added `ForgeQueryPlanWarmup` so applications can warm query plans and binders at startup without executing SQL.

Use this for key endpoints:

```csharp
forgeDb.WarmupQuery<Order>(
    "SELECT Id, OrderNo, Status FROM dbo.Orders WHERE Status = @Status",
    new { Status = OrderStatus.Paid });
```

## Enterprise Positioning

ForgeORM now has a stronger foundation for:

- Dapper-like raw SQL performance
- EF-like diagnostics and interceptors
- source-generated materialization
- MSIL fallback
- typed parameter binding
- stream-first large result handling
- graph/bulk/query-analytics features
- production observability

## Next Recommended Step

Add provider-specific packages to register advanced optimizers:

```text
ForgeORM.SqlServer
  - TVP binder
  - SqlBulkCopy executor
  - SqlDataReader typed materializers
  - prepared command strategy

ForgeORM.PostgreSql
  - COPY executor
  - ANY(@ids) list binder
  - NpgsqlDataReader typed materializers

ForgeORM.MySql
  - MySqlBulkCopy / LOAD DATA path
  - expanded IN binder

ForgeORM.Oracle
  - array binding
  - OracleDataReader typed materializers
```
