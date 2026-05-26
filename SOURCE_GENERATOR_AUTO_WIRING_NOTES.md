# ForgeORM Source Generator Auto Wiring Fix

The source generator is now designed to be self-wiring.

## User configuration

The user only needs:

```csharp
builder.Services.AddForgeOrm(options =>
{
    options.UseSqlServer(connectionString);
    options.UseCompilationMode(ForgeOrmCompilationMode.SourceGenerated);
});
```

No manual registry calls, no analyzer paths, and no package-target configuration are required.

## NuGet consumer behavior

`ForgeORM.Core` and `ForgeORM.AspNetCore` pack `ForgeORM.SourceGenerators.dll` under:

```text
analyzers/dotnet/cs/ForgeORM.SourceGenerators.dll
```

The package also includes `buildTransitive/*.props`, which auto-adds that analyzer for consuming projects.

## Source-tree/sample behavior

When the sample/API/benchmark projects reference ForgeORM projects directly instead of consuming NuGet, `Directory.Build.targets` attaches the generator through:

```xml
<ProjectReference OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

This removes the old brittle behavior:

- no hardcoded `bin/Debug/netstandard2.0` analyzer reference in consumers
- no `GetTargetPath` target call
- no File-not-found error before the generator is built

## Runtime behavior

`SourceGenerated` mode remains strict:

- generated provider/reader is used
- runtime emit is not used
- if the generator did not produce a reader, ForgeORM throws a clear error

`Auto` mode remains flexible:

- source-generated reader first
- runtime emit fallback only when generated reader is unavailable
