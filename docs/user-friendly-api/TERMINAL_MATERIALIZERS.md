# User-Friendly Terminal Materializers

ForgeORM builders should be powerful but simple.

The rule:

```text
ToXxxAsync = Render SQL + Execute + Read + Materialize
```

Users should not manually render SQL, call a second query method, serialize JSON or map dictionary rows for reports.

## Reporting

### JSON result

```csharp
var result = await db.Report<Order>("TopCustomers")
    .From("dbo.Orders")
    .Dimension("CustomerId", "CustomerId")
    .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
    .TopN("Revenue", 10)
    .ToJsonAsync(ct);
```

### Dictionary rows

```csharp
var rows = await db.Report<Order>("TopCustomers")
    .From("dbo.Orders")
    .Dimension("CustomerId", "CustomerId")
    .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
    .TopN("Revenue", 10)
    .ToDictionaryAsync(ct);
```

### DataFrame-friendly table

```csharp
var frame = await db.Report<Order>("TopCustomers")
    .From("dbo.Orders")
    .Dimension("CustomerId", "CustomerId")
    .Measure(ForgeReportMeasure.Sum("GrandTotal", "Revenue"))
    .TopN("Revenue", 10)
    .ToDataFrameAsync(ct);
```

### CSV

```csharp
var csv = await report.ToCsvAsync(ct);
```

## Dynamic shape warning

Pivots and unpivots should use:

```csharp
ToDictionaryAsync()
ToJsonAsync()
ToDataFrameAsync()
```

Do not use `ToListAsync<Order>()` for dynamic pivot columns because pivot columns do not exist on the `Order` entity.

## SQL-only preview

```csharp
var sql = report.ToSqlProjection();
```

Use this only for debugging, previewing and documentation.
