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


# IDE and Compiler Integration Layer

## Schema-aware SQL

```csharp
var users = await db.SchemaSql<User>($"SELECT Id, Name FROM Users WHERE Id = {userId}")
    .ToListAsync();
```

The `ForgeSqlInterpolatedStringHandler` captures parameters safely and prepares the API for Roslyn analyzer validation.

## Ghost Projections

```csharp
var users = await db.SmartSql<User>("SELECT * FROM Users")
    .SelectAutomatic()
    .ToListAsync();
```

The runtime method is a no-op today; the intended analyzer/source-generator tracks property usage and rewrites SELECT columns.

## Trace Visualizer

```csharp
var trace = db.SmartSql<User>("SELECT * FROM Users")
    .TraceVisualizer(visualizer);
```

Returns a local trace URL contract containing SQL, parameters, provider name and hot-path warnings.

## API-to-DB Reflection

```csharp
var query = reflector.ReflectRequest<User>(httpContext);
```

Converts query string filters into parameterized SQL. Production version should validate against indexed columns.

## Semantic Search

```csharp
var query = semantic.SearchSemantic<User>("Bio", "experienced coder");
```

Provider-specific implementations can use pgvector, SQL Server vector search, Azure AI Search or other embedding backends.
