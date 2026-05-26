# ForgeORM Source Generator Zero-Config Fix v21

Fixes MSB4057: target ResolveReferences does not exist.

## What changed

- Removed DependsOnTargets="ResolveReferences" from analyzer pack targets.
- Added internal ProjectReference to ForgeORM.SourceGenerators with OutputItemType=Analyzer.
- Source generator is built by MSBuild as part of package build.
- NuGet package includes analyzers/dotnet/cs/ForgeORM.SourceGenerators.dll.
- Consumer user still does not add build targets, analyzer paths, or per-entity registrations.

## User setup

```csharp
builder.Services.AddForgeOrm(options =>
{
    options.UseSqlServer(connectionString);
    options.UseCompilationMode(ForgeOrmCompilationMode.SourceGenerated);
});
```

That is the only required user configuration.
