# ForgeORM Source Generator Path Fix v16

The CS0006 error came from hardcoded analyzer DLL paths like:

```text
src/ForgeORM.SourceGenerators/bin/Debug/netstandard2.0/ForgeORM.SourceGenerators.dll
```

Those paths are now removed from consumer compilation. Core/AspNetCore packaging uses a packaging-only glob after building the generator and never adds that path as an Analyzer during normal compile.

For source-tree samples/benchmarks, `Directory.Build.targets` uses `ProjectReference OutputItemType=Analyzer`, so MSBuild builds and passes the real generator output automatically.

After applying this patch, run:

```powershell
dotnet clean
rd /s /q src\ForgeORM.SourceGenerators\bin src\ForgeORM.SourceGenerators\obj
dotnet restore
dotnet build
```
