# SourceGenerated Mode Fix

This patch corrects the SourceGenerated execution model:

- `SourceGenerated` mode is now treated as a compile-time generation mode, not a cache lookup followed by RuntimeEmit.
- The consumer project now references `ForgeORM.SourceGenerators` as an analyzer, so generated providers are produced during build and registered through `ModuleInitializer`.
- The generated provider now returns typed delegates directly through `CreateTypedReader_*`.
- `TryCreateReader<T>` casts the generated delegate once instead of wrapping `Func<DbDataReader, object>` and casting every row.
- RuntimeEmit fallback remains disabled when `SourceGenerated` or `SourceGeneratedStrict` is selected.

Correct flow:

```csharp
SourceGenerated selected
        ↓
Analyzer generates ForgeORM.Generated.ForgeGeneratedAccessorProvider
        ↓
ModuleInitializer registers provider in ForgeSourceGeneratedRegistry
        ↓
Runtime resolves provider from registry/cache
        ↓
Generated typed reader executes
        ↓
No RuntimeEmit fallback
```

Important: Source generation happens at build time. Therefore the project that owns the entity/DTO types must reference the generator as an analyzer:

```xml
<ProjectReference Include="..\..\src\ForgeORM.SourceGenerators\ForgeORM.SourceGenerators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  PrivateAssets="all" />
```

For NuGet consumers this should be provided by the `ForgeORM.SourceGenerators` analyzer package.
