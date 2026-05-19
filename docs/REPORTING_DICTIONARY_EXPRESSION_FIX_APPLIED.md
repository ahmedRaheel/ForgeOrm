# Reporting Dictionary + Expression API Fix Applied

Fixed issue where report pivot SQL returned data in SQL Server but `ToList` / dictionary materialization returned empty/default rows.

## Root cause

Pivot/report queries return dynamic projection columns that do not map back to the source entity type such as `Order`.

Example pivot-like grouped output:

```text
PivotRow | PivotColumn | Revenue
```

This cannot be materialized into `Order` safely.

## Fixes

Added to `ForgeDb`:

- `QueryDictionaryAsync(...)`
- `QueryDictionary(...)`

Updated `ForgeReportBuilder<T>`:

- `ToListAsync()` returns `IReadOnlyList<Dictionary<string, object?>>`
- `ToDictionaryProjectionAsync(...)`
- `ToDictionaryListAsync(...)`
- `ToDictionaryProjection()` remains as SQL alias for compatibility
- `ExportCsvAsync(...)` and `ExportExcelAsync(...)` now use dictionary materialization

Added expression overloads for reporting methods:

- `Dimension(name, expression)`
- `Year(name, expression)`
- `Month(name, expression)`
- `Day(name, expression)`
- `Sum(expression, alias)`
- `Count(expression, alias)`
- `Average(expression, alias)`
- `Min(expression, alias)`
- `Max(expression, alias)`
- `OrderBy(expression)`
- `OrderByDescending(expression)`
- `TopN(expression, count)`
- `Pivot(row, column, value)`
- `PivotByYear(date, column, value)`
- `Unpivot(nameColumn, valueColumn, expressions...)`
- `Window(function, expression, alias)`
- `RowNumber(alias, partitionBy, orderBy)`
- `Percentile(expression, percentile, alias, partitionBy)`
- `RollingAverage(expression, orderBy, precedingRows, alias, partitionBy)`
- `DrillDown(name, expression)`

Updated sample endpoints under `/reporting`:

- SQL and expression versions for pivot
- SQL and expression versions for unpivot
- SQL and expression versions for window functions
- SQL and expression versions for percentile
- SQL and expression versions for top-n
- SQL and expression versions for drill-down
- SQL and expression versions for CSV export
- SQL and expression versions for Excel export

## Correct usage

```csharp
var report = db.Report<Order>("SalesPivot")
    .From("dbo.Orders")
    .PivotByYear(x => x.CreatedAt, x => x.Status, x => x.GrandTotal, "SUM", "Revenue");

var sql = report.ToSql();
var rows = await report.ToListAsync(ct);
```
