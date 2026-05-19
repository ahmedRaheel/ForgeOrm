# Build Stability / Report Pivot Fix Applied

Fixed:
- Removed duplicate `ForgeReportBuilder<T>.Pivot(string,string,string,string,string)` overload.
- Kept one string pivot overload with friendly named arguments:
  `.Pivot(row: "...", column: "...", value: "...", aggregate: "SUM", alias: "Revenue")`
- Kept expression pivot as:
  `.PivotExpr(row: x => x.CreatedAt.Year, column: x => x.Status, value: x => x.GrandTotal, ...)`
- Added `src/ForgeORM.Querying/ForgeORM.Querying.csproj`.
- Added sample project reference to `ForgeORM.Querying`.
- Updated sample using from `ForgeORM.Core.Search` to `ForgeORM.Querying.Search`.
- Verified no extension method still calls private `db.CreateConnection()`.

Note:
- `ForgeORM.Querying` was added as a project folder and referenced by the sample project.
- If you open the `.sln` and it does not appear visually, add the existing project from `src/ForgeORM.Querying/ForgeORM.Querying.csproj`.
