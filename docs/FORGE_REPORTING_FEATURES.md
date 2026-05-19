# ForgeORM Reporting Features

Added reporting-builder support for:

- Pivot()
- Unpivot()
- Window()
- Percentile()
- RollingAverage()
- TopN()
- DrillDown()
- DrillThrough()
- ExportExcel()
- ExportCsv()

## Example

```csharp
var report = db.Report<Order>("MonthlySales")
    .From("dbo.Orders")
    .Dimension("Year", "YEAR(CreatedAt)")
    .Dimension("Status", "Status")
    .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
    .Pivot("YEAR(CreatedAt)", "Status", "GrandTotal", "SUM", "Revenue")
    .RollingAverage("GrandTotal", "CreatedAt", 6, "RollingAverage")
    .TopN("Revenue", 10)
    .DrillDown("Month", "MONTH(CreatedAt)")
    .DrillThrough("OrderDetails", "SELECT * FROM dbo.Orders WHERE CustomerId = @CustomerId")
    .Where("GrandTotal > @Min", new { Min = 100 });

var sql = report.ToSql();
var rows = await report.ToListAsync<dynamic>(ct);
var csv = await report.ExportCsvAsync(ct);
var excel = await report.ExportExcelAsync("MonthlySales", ct);
```
