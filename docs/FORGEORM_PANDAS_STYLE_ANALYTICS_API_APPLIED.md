# ForgeORM Pandas-Style Analytics API Applied

This patch adds a Pandas-inspired analytics API to `ForgeORM.DataFrame`:

- `ForgePandas.DataFrame(...)`, `ForgePandas.Series(...)`
- `ReadCsv`, `ReadCsvAsync`, `ToCsv`, `ToCsvAsync`
- `ReadExcel`, `ToExcel` using a dependency-free minimal XLSX implementation
- `ToDateTime`
- `Head`, `Tail`, `Info`, `Describe`, `Shape`
- `ValueCounts`, `Unique`, `NUnique`
- `Loc`, `ILoc`, `SortValues`, `Query`
- `IsNull`, `NotNull`, `DropNaPandas`, `FillNaPandas`, `DropDuplicates`, `Rename`
- `GroupBy().Agg(...)`, `Apply`, `ApplyColumns`
- `Concat`, `Merge`, `PivotTable`
- `Plot` returns a lightweight `ForgePlotSpec` for UI rendering

Example:

```csharp
var df = ForgePandas.ReadCsv("orders.csv")
    .ToDateTime("CreatedAt")
    .Query("Status == 'Paid' and GrandTotal >= 100")
    .SortValues("CreatedAt", ascending: false);

var summary = df
    .GroupBy("Status")
    .Agg(
        ForgeAggregation.Count(alias: "Orders"),
        ForgeAggregation.Sum("GrandTotal", "Revenue"));

var pivot = df.PivotTable(
    index: "CustomerId",
    columns: "Status",
    values: "GrandTotal",
    aggFunc: "sum");
```
