# DataFrame API Placement Rules

All Pandas/DataFrame functions must live in the DataFrame project, not endpoints.

## Canonical access

Preferred public surface:

```csharp
var frame = db.DataFrame(rows);
var result = frame
    .FillNa(0)
    .StrUpper("Region")
    .RollingMean("Revenue", 2, "RevenueRolling2")
    .CumSum("Revenue", "RevenueCumSum")
    .RankValues("Revenue", "RevenueRank", ascending: false);
```

## Endpoint rule

Endpoints may demonstrate usage only. They must not define DataFrame behavior.

## Ambiguity rule

Do not define duplicate public extension methods with the same effective signature in multiple classes.

Avoid duplicate methods across:

- ForgePandasExtensions
- ForgePandasCheatSheetExtensions
- endpoint helper classes

Keep one canonical implementation, preferably directly on `ForgeDataFrame` or in one internal extension layer only.
