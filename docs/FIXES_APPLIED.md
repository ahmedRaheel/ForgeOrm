# ForgeORM Compile/Merge Fixes Applied

## Fixed compile-blocking issues found by static inspection

1. **Graph executor return type mismatch**
   - Updated `src/ForgeORM.Core/Graph/IForgeGraphExecutor.cs` so provider executors correctly implement:
     - `Task<ForgeGraphResult> InsertGraphAsync(...)`
     - `Task<ForgeGraphResult> UpdateGraphAsync(...)`
     - `Task<ForgeGraphResult> DeleteGraphAsync(...)`
   - This matches `ForgeGraphService` and all provider executors.

2. **Broken provider project references**
   - Fixed all provider `.csproj` files that incorrectly referenced:
     - `..\ForgeORM.Abstractions\ForgeORM.Core.csproj`
   - Corrected to:
     - `..\ForgeORM.Core\ForgeORM.Core.csproj`

3. **Graph options completed**
   - Added/defaulted:
     - `ChildSyncMode = InsertUpdateDeleteMissing`
     - `DeleteMode = HardDelete`
     - `SoftDeleteColumn = "IsDeleted"`

4. **Removed stale duplicate/orphan folder**
   - Removed non-project experimental folder:
     - `src/ForgeORM.SqlServer/Graph`
   - The valid provider is:
     - `src/ForgeORM.Providers.SqlServer/Graph`

5. **Cleaned generated artifacts**
   - Removed `bin/` and `obj/` folders from the returned package to avoid stale restore/build cache issues.

## Verified by static checks

- All `.csproj` files are valid XML.
- All `ProjectReference` targets exist.
- Central Package Management rule checked: no direct `Version=` attributes on `PackageReference` entries.
- Enterprise query/dataframe files are present.
- Graph persistence files are present.

## Important note

`dotnet build` could not be executed in this environment because the .NET SDK is not installed here. Run this locally:

```bash
dotnet clean ForgeORM.sln
dotnet restore ForgeORM.sln
dotnet build ForgeORM.sln -c Release
```
