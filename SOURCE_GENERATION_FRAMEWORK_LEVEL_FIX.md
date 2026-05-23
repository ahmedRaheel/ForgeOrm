# Source Generation Framework-Level Fix

Applied globally, not per method.

## What changed

- Added `SourceGeneratedStrict` for fail-fast NativeAOT scenarios.
- `SourceGenerated` now means generated-first with safe provider/MSIL fallback. This prevents runtime failures like `No ForgeORM source-generated reader was registered for ...` when a benchmark/sample projection was not generated.
- `ReflectionForgeEntityMetadataResolver` now respects generated metadata first, so older setup code still benefits from generated metadata.
- Source generator now supports convention-based model/projection discovery for `.Models`, `.Dtos`, `.Projections`, and `*Dto`, `*Record`, `*Projection` types. You no longer need attributes on every benchmark/sample model.
- Benchmark and sample projects now reference `ForgeORM.SourceGenerators` as an Analyzer.

## Recommended benchmark mode

Use:

```csharp
ForgeSourceGeneratedRegistry.CompilationMode = ForgeOrmCompilationMode.SourceGenerated;
```

Use strict mode only when you want missing generated artifacts to throw:

```csharp
ForgeSourceGeneratedRegistry.CompilationMode = ForgeOrmCompilationMode.SourceGeneratedStrict;
```
