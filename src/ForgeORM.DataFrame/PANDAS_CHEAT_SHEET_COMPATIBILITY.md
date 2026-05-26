# ForgeORM DataFrame Pandas-Cheat-Sheet Compatibility

ForgeORM.DataFrame now exposes Pandas-inspired analytics operations directly over `ForgeDataFrame`.

## Import / Export
- `FromCsv`, `FromCsvAsync`, `FromCsvText`
- `FromJson`, `FromJsonAsync`, `FromJsonText`
- `ToTable`, `ToTableAsync`
- `ToMicrosoftDataFrame`
- existing CSV/HTML export helpers

## Inspect / View
- `Shape()`
- `Info()`
- `DTypes()`
- `ColumnsFrame()`
- `Head()`
- `Tail()`
- `Describe()`
- `CountNonNull()`

## Select / Index
- `SelectColumns(...)`
- `DropColumns(...)`
- `Column("Name")`
- `Iloc(start, end)`
- `At(rowIndex, column)`
- `ResetIndex()`
- `SetIndex(column)`

## Filter / Sort / Sample
- `FilterRows(...)`
- `WhereEquals(column, value)`
- `Between(column, min, max)`
- `IsIn(column, values...)`
- `SortValues("Column", "-DescendingColumn")`
- `NLargest(n, column)`
- `NSmallest(n, column)`
- `Sample(n, seed)`

## Create / Mutate Columns
- `Assign(...)`
- `WithColumn(...)`
- `MapColumn(...)`
- `ReplaceValues(...)`
- `CastColumn(...)`
- `ToNumeric(...)`
- `ToDateTime(...)`
- `Clip(...)`

## Missing Data
- `FillNulls(value, columns...)`
- `DropNulls(columns...)`
- `IsNullFrame()`
- `NotNullFrame()`
- `DropDuplicates(...)`

## Group / Aggregate
- `GroupBy(...).Agg(...)`
- `ValueCounts(column)`
- `Unique(column)`
- `NUnique(column)`
- `PivotTable(...)`

## Reshape / Combine
- `Melt(...)`
- `Merge(...)`
- `MergeOn(...)`
- `ConcatRows(...)`
- `ConcatColumns(...)`

## Window / Time Series Style Analytics
- `Shift(...)`
- `Diff(...)`
- `CumSum(...)`
- `CumCount(...)`
- `RollingSum(...)`
- `RollingMean(...)`
- `ExpandingSum(...)`
- `ExpandingMean(...)`

## String / Date Helpers
- `StringContains(...)`
- `StringLower(...)`
- `StringUpper(...)`
- `StringTrim(...)`
- `StringReplace(...)`
- `DateYear(...)`
- `DateMonth(...)`
- `DateDay(...)`

## Example

```csharp
var frame = ForgeDataFrame.FromCsv("orders.csv")
    .ToNumeric("GrandTotal")
    .StringTrim("Status")
    .WhereEquals("Status", "Paid")
    .WithColumn("Tax", r => (decimal?)ForgeDataFrame.Get(r, "GrandTotal") * 0.15m)
    .RollingMean("GrandTotal", 7, "Rolling7DayAverage");

var report = frame
    .GroupBy("CustomerId")
    .Agg(
        ForgeAggregation.Count(alias: "Orders"),
        ForgeAggregation.Sum("GrandTotal", "Revenue"),
        ForgeAggregation.Avg("GrandTotal", "AvgOrder"));
```
