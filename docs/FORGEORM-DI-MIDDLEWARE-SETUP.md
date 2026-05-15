# ForgeORM DI / Middleware-Style Setup

Samples must not manually register ForgeORM infrastructure services.

Use one high-level registration:

```csharp
builder.Services.AddForgeOrm(options =>
    options.UseSqlServer(connectionString));
```

`AddForgeOrm(...)` now registers internally:

- `ForgeDbContext`
- `IForgeDb`
- provider abstraction
- metadata resolver
- query analyzer
- dynamic query builders
- AST dynamic query builder
- artifact manager
- schema manager
- object mapper
- SQL intelligence
- trace/semantic/next-gen services

The sample project should only inject public services:

```csharp
app.MapPost("/artifacts/view/product-list", async (
    ForgeDbContext db,
    IForgeArtifactManager artifacts) =>
{
    // user-facing example only
});
```

No manual infrastructure wiring should be required from consumers.
