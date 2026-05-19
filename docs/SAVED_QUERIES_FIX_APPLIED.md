# Saved Queries Fix Applied

Added missing Saved Queries support used by sample code:

- `ForgeSavedQuery`
- `ForgeSavedQueryRegistry`
- `ForgeSavedQueryManager`
- `ForgeSavedQueryExtensions`
- `ForgeDbContext.SavedQueries` property
- Sample endpoint module: `SavedQueryEndpoints`
- Program registration: `app.MapSavedQueryEndpoints();`

Usage:

```csharp
db.SavedQueries.Register(
    "HighValueOrders",
    "SELECT * FROM dbo.Orders WHERE GrandTotal >= @MinTotal");

var rows = await db.SavedQueries.ExecuteAsync<OrderSummaryRecord>(
    "HighValueOrders",
    new { MinTotal = 1000m },
    ct);
```
