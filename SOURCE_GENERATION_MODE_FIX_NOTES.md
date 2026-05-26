# SourceGenerated Mode Fix

This patch makes this the only configuration required at runtime:

```csharp
builder.Services.AddForgeOrm(options =>
{
    options.UseSqlServer(connectionString);
    options.UseCompilationMode(ForgeOrmCompilationMode.SourceGenerated);
});
```

Behavior:

- `SourceGenerated` means generated provider/materializer only.
- `RuntimeEmit` means emit only.
- `Auto` tries generated first, then fallback.
- `AddForgeOrm` now configures the compilation mode and discovers generated providers automatically.
- `ForgeORM.AspNetCore` packages `ForgeORM.SourceGenerators` as an analyzer so NuGet consumers do not need a separate analyzer reference.

Important compile-time fact: source generation happens at build time. The DI call selects the generated path at runtime; the package now carries the analyzer so the generated provider exists without extra user setup.
