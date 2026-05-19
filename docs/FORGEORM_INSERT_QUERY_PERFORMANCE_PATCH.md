# ForgeORM Insert + Query Performance Patch

Applied fixes:

- Insert/update metadata now uses scalar database columns only.
- Navigation properties such as `Customer` and `List<OrderItem> Items` are excluded from INSERT/UPDATE column lists.
- Bulk insert/update fallback also excludes navigation properties.
- Default `DateTime`/`DateTimeOffset` values are normalized before database writes to avoid SQL Server datetime overflow.
- Query materialization now uses cached compiled setters instead of `PropertyInfo.SetValue` per row.
- Query parameter binding now caches parameter properties and ignores navigation properties.
- Expression query SELECT generation now emits explicit scalar columns instead of `SELECT *`.
- `FirstOrDefaultAsync` raw execution now uses single-row reader behavior instead of materializing a full list.
- Paging keeps `ORDER BY 1` fallback and normalizes `skip == take` / invalid take values.

Expected result:

- Insert should no longer fail with invalid columns `Customer` or `Items`.
- Query-by-id and first/default paths should allocate less and run faster.
- Paging queries avoid navigation-property columns and unnecessary list growth.
