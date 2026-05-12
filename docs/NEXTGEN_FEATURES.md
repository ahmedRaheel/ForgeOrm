# ForgeORM NextGen API Update

## Added APIs

- `SmartSql<T>(FormattableString sql)` for safe interpolated SQL
- `WhereSql(FormattableString sql)` for protected raw SQL fragments
- `ToShape<TShape>()` / `ToShapeAsync<TShape>()` projection contract
- `MapStatic<TShape>()` source-generator mapping contract
- `IntoJson()` / `IntoJsonDocument()` JSON-first API
- `StreamAllAsync()` async streaming API
- `Explain()` execution-plan command builder
- `AsCached()` memory-cache integration
- `Mock()` in-memory query mode
- `ExecuteTransparent()` debug SQL preview
- `WithPolicy()` retry/circuit policy hook
- `GenerateDiff()` / `VerifySchema()` / `SyncSchema()` schema-evolution contracts
- `IncludeGraph()` graph-loading contract
- `ShadowProperty()` metadata-column contract

## Example

```csharp
var rows = await db.SmartSql<Product>($"SELECT Id, Code, Name, Price FROM Products WHERE Price > {minPrice}")
    .WhereSql($"Name <> {""}")
    .AsCached(TimeSpan.FromMinutes(5))
    .WithPolicy(new ForgeResiliencePolicy { RetryCount = 2 })
    .ToShapeAsync<ProductDto>();
```
