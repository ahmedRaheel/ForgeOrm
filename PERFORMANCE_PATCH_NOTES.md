# ForgeORM Performance Patch Notes

Applied in this ZIP:

1. SQL Server direct GetById plan no longer uses LINQ to find key columns or build SELECT projections.
2. SQL Server direct query parameter tokenization is Regex-free and LINQ-free, using span/loop scanning.
3. SQL Server enumerable `IN @Ids` expansion now uses a span parser instead of Regex.
4. SQL Server dictionary projection now caches column names once per reader shape instead of calling `GetName(i)` per row.
5. SQL Server direct materializer emits concrete typed getters such as `GetInt32`, `GetString`, `GetDecimal`, `GetDateTime`, etc. before falling back to `GetFieldValue<T>`.
6. Provider-neutral MSIL materializer also emits concrete typed `DbDataReader` getters before falling back to `GetFieldValue<T>`.
7. Source-generated list query executor is now attempted before runtime materializer fallback in the async SQL Server direct query path.
8. Runtime entity metadata cache no longer uses LINQ for scalar property filtering, key discovery, insert/update/select SQL construction.
9. SQL Server parameter binder cache no longer uses LINQ/OrderBy/ToDictionary for binder-key and property map construction.
10. Existing ValueTask-based hot APIs remain preserved and the direct SQL Server async query path uses ValueTask-based source-generated executors when registered.

Note: I could not run `dotnet build` in this environment because the .NET SDK is not installed in the sandbox. The patch was applied directly to source files and brace/syntax structure was checked textually.

## Allocation patch v2 — typed key hot path

Merged additional Query_By_Id allocation fixes focused on the remaining ~17.3 KB allocation profile:

- Added typed SQL Server direct GetById executor overloads: `Execute<TKey>` and `ExecuteAsync<TKey>`.
- Added per-entity + per-key-type static executor plan cache via `TypedPlanCache<TKey>`.
- Added provider-direct typed API: `ForgeSqlServerProviderDirectHotPath.GetById<T,TKey>` and async equivalent.
- Updated `ForgeDb.GetById<T,TKey>` to call the typed provider-direct path instead of the object-key overload.
- Added typed `Find<T,TKey>` / `FindAsync<T,TKey>` benchmark APIs so `Find<Order,int>(id)` avoids object-key boxing at the public API and executor dispatch level.
- Kept object overloads for compatibility, but benchmark hot paths should prefer typed-key overloads.

Recommended benchmark target:

```csharp
[Benchmark]
public Order? ForgeORM_Query_By_Id()
{
    return _db.Find<Order, int>(_id);
    // or: return _db.GetById<Order, int>(_id);
}
```

Avoid this in the benchmark path because it boxes the key before ForgeORM can help:

```csharp
_db.Find<Order>(_id);
_db.GetById<Order>(_id);
```
