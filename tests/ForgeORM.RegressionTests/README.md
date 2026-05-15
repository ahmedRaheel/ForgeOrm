# ForgeORM Regression Tests

This lightweight test harness is intentionally package-free so it does not require changes to `Directory.Packages.props`.

Run:

```bash
dotnet run --project tests/ForgeORM.RegressionTests/ForgeORM.RegressionTests.csproj
```

Covered regressions:

- CSV null-like values such as `?`, `NA`, `N/A`, `nan` become `null`.
- JSON null-like string values become `null`.
- CSV and JSON stream import overloads work.
- Numeric CSV headers such as `1980`, `1981` are preserved for DataFrame operations.
- Microsoft.Data.Analysis bridge remains usable.
