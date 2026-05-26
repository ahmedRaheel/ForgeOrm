# ForgeORM RuntimeEmit-only patch

This patch removes the failing source-generator/analyzer path and makes MSIL RuntimeEmit the single active materialization strategy.

## Removed

- `src/ForgeORM.SourceGenerators`
- `src/ForgeORM.SourceGenerated`
- source-generator project entries from `ForgeORM.sln`
- analyzer packaging targets from `ForgeORM.Core.csproj` and `ForgeORM.AspNetCore.csproj`
- buildTransitive analyzer props
- runtime generated-provider discovery paths
- source-generated query executor checks from hot query paths

## Runtime behavior

- `Auto` maps to RuntimeEmit.
- `RuntimeEmit` uses MSIL DynamicMethod materializers.
- legacy `SourceGenerated` and `SourceGeneratedStrict` enum values are retained only for source compatibility and map to RuntimeEmit in DI.
- SQL Server direct hot path uses `Func<SqlDataReader,T>` emitted materializers.
- No analyzer DLL, build target, project reference, or manual registration is required.

## Performance fix

`ForgeReaderShapeCache.CreateKey` no longer calls `GetSchemaTable()` for materializer cache keys. That avoids a large allocation/regression in `QueryFirstOrDefault` and `GetById` hot paths.
