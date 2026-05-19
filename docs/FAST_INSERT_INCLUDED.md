# Fast Insert Included

Added `src/ForgeORM.Core/ForgeDb.InsertFast.cs`.

APIs:

```csharp
int InsertFast<T>(T entity);
Task<int> InsertFastAsync<T>(T entity, CancellationToken cancellationToken = default);
```

Behavior:

- Uses cached insert SQL per entity type.
- Uses compiled property getters.
- Inserts scalar DB columns only.
- Excludes identity/key columns like `Id`.
- Ignores navigation properties such as `Customer` and `Items`.
- Normalizes invalid/default `DateTime` and `DateTimeOffset` values.
- Converts enum values to string by default, or number when `[ForgeEnumStorage(ForgeEnumStorage.Number)]` is used.

Use `InsertGraphAsync` for aggregate graph inserts. Use `InsertFastAsync` for single-row high-throughput benchmarks.
