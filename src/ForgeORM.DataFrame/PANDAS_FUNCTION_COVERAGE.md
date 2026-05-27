# ForgeORM DataFrame Pandas Function Coverage

This package maps the uploaded Pandas reference into ForgeORM DataFrame APIs.

## Creation
- `ForgePandas.Series(...)`
- `ForgePandas.DataFrame(...)`
- `ForgePandas.FromDict(...)`
- `ForgePandas.JsonNormalize(...)`
- `ForgePandas.DateRange(...)`
- `ForgePandas.PeriodRange(...)`
- `ForgePandas.TimedeltaRange(...)`
- `ForgePandas.Timestamp(...)`
- `ForgePandas.Timedelta(...)`
- `ForgePandas.ToDateTime(...)`
- `ForgePandas.ToTimedelta(...)`

## Reading
- `ForgePandas.ReadCsv(...)`
- `ForgePandas.ReadTable(...)`
- `ForgePandas.ReadFwf(...)`
- `ForgePandas.ReadExcel(...)`
- `ForgePandas.ReadJson(...)`
- `ForgePandas.ReadSqlAsync(...)`
- `ForgePandas.ReadHtml(...)`
- `ForgePandas.ReadXml(...)`
- `ForgePandas.ReadPickle(...)` as portable JSON-backed equivalent

## Writing
- `frame.ToCsv(...)`, `frame.ToCsvText(...)`
- `frame.ToExcel(...)`
- `frame.ToJson(...)`, `frame.ToJsonText(...)`
- `frame.ToHtml(...)`
- `frame.ToXml(...)`
- `frame.ToMarkdown(...)`
- `frame.ToDict(...)`
- `frame.ToNumpy(...)`
- `frame.ToTable(...)`, `frame.ToTableAsync(...)`

## Inspection
- `Head`, `Tail`, `Sample`, `Info`, `Describe`, `Shape`, `Size`, `NDim`, `Columns`, `Index`, `DTypes`, `MemoryUsage`, `ValueCounts`, `Unique`, `NUnique`

## Selection and filtering
- `Select`, `Loc`, `ILoc`, `At`, `IAt`, `Query`, `Where`, `WhereMask`, `Mask`, `Filter`, `IsIn`, `BetweenTime`

## Missing values
- `IsNull`, `NotNull`, `DropNa`, `FillNa`, `Interpolate`, `Replace`

## Sorting and reshaping
- `SortValues`, `SortIndex`, `Rename`, `RenameAxis`, `SetIndex`, `ResetIndex`, `Reindex`, `Transpose`, `T`

## Combining
- `Concat`, `Merge`, `Join`, `Combine`, `CombineFirst`

## Grouping and aggregation
- `GroupBy`, `Agg`, `Aggregate`, `Transform`, `Apply`, `ApplyMap`, `MapColumn`, `Sum`, `Mean`, `Median`, `Mode`, `MinValue`, `MaxValue`, `Count`, `Size`, `Std`, `Var`, `Sem`, `Prod`, `Quantile`, `IdxMin`, `IdxMax`

## Window functions
- `Rolling`, `RollingMean`, `RollingSum`, `RollingStd`, `RollingVar`, `ExpandingMean`, `ExpandingSum`, `EwmMean`

## String/date/math/statistics
- `StrLower`, `StrUpper`, `StrTitle`, `StrStrip`, `StrReplace`, `StrContains`, `StrStartsWith`, `StrEndsWith`, `StrSplit`, `StrExtract`, `StrLen`
- `DtYear`, `DtMonth`, `DtDay`, `DtHour`, `DtMinute`, `DtSecond`, `DtWeekday`, `DtDayName`, `DtMonthName`
- `Abs`, `Round`, `Clip`, `CumSum`, `CumProd`, `CumMax`, `CumMin`, `Diff`, `PctChange`, `RankValues`
- `Corr`, `Cov`, `Skew`, `Kurt`, `Mad`

## Pivot/crosstab/duplicates/types/time/category
- `PivotTable`, `Pivot`, `Melt`, `Stack`, `Unstack`, `CrossTab`
- `Duplicated`, `DropDuplicates`
- `Astype`, `ConvertDTypes`, `InferObjects`
- `Resample`, `AsFreq`, `Shift`, `TzLocalize`, `TzConvert`
- `ToCategory`, `CatCategories`, `CatCodes`, `CatRenameCategories`, `CatAddCategories`, `CatRemoveCategories`

## Utility and testing
- `MultiIndexFromArrays`, `MultiIndexFromTuples`, `MultiIndexFromProduct`
- `GetDummies`, `Factorize`, `Cut`, `QCut`, `Unique`, `ValueCounts`
- `Eval`, `Pipe`, `All`, `Any`, `Bool`
- `Copy`, `Insert`, `Pop`, `Drop`, `EqualsFrame`, `CompareFrame`, `Explode`
- `AssertFrameEqual`, `ForgeInterval`, `IntervalRange`, `ForgeSparseDType`, `ForgeExcelFile`, `ForgeExcelWriter`

## Demo endpoint
Run the sample API and open:

`GET /dataframe/pandas-complete/demo`
