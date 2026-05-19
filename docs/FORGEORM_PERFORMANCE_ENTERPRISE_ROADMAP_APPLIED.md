# ForgeORM Performance + Enterprise Roadmap Applied

This update adds a first-class performance architecture around source-generated readers, MSIL fallback materializers, cached parameter binders, metadata/query plan caching, provider strategy hooks, benchmark coverage, production samples, documentation website content, vector/AI messaging and marketing/tutorial assets.

## Hot path rule
Reflection is allowed only during cache construction. Runtime query calls should use this order:

1. Source-generated reader or parameter binder registered in `ForgeGeneratedRegistry`.
2. Cached MSIL `DynamicMethod` reader/binder.
3. Safe database/provider fallback only for unsupported shapes.

## Included folders

- `src/ForgeORM.SourceGenerators` — analyzer package that emits reader and binder registration for `[ForgeTable]` classes.
- `src/ForgeORM.Core/ForgeGeneratedRegistry.cs` — runtime registry used by generated code.
- `benchmarks/ForgeORM.Benchmarks` — BenchmarkDotNet project expanded for reflection vs MSIL hot paths.
- `docs-site` — Vite/React documentation website.
- `samples/ForgeORM.Production.Sample` — production-ready sample notes and endpoints.
- `marketing` and `tutorials` — launch content, LinkedIn copy and hands-on tutorials.

## Next hardening pass

Run these locally after restoring packages:

```bash
dotnet restore ForgeORM.sln
dotnet build ForgeORM.sln -c Release
dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks
cd docs-site && npm install && npm run build
```
