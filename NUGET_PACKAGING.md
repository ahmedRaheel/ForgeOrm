# NuGet Packaging

This repository is configured for NuGet packaging.

## Build packages

```bash
dotnet pack ForgeORM.CompleteAllFeatures.sln -c Release
```

Packages are generated under each project `bin/Release` folder.

## Publish

```bash
dotnet nuget push "**/*.nupkg" --api-key YOUR_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

## Notes

- Source projects are packable.
- Sample project is marked `IsPackable=false`.
- Shared README and icon are included in every source package.
- Version: `1.0.0-preview.1`.
