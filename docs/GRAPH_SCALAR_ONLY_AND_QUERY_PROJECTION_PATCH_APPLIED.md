# Graph scalar-only + query projection patch applied

Applied fixes:

- Graph insert/update/delete now use `ForgeEntityShape.ScalarProperties` that include only scalar database columns.
- Reference navigation properties such as `Customer` are no longer treated as database columns.
- Collection navigation properties such as `Items` remain graph navigations only.
- Provider `BuildGetById`, `BuildGetByCode`, and `BuildGetByIds` now project explicit scalar columns instead of `SELECT *`.
- Child graph fetch uses scalar child columns instead of `SELECT *`.

This fixes errors like:

```text
Invalid column name 'Customer'
Invalid column name 'Items'
```

Default query remains parent-only. Explicit `Include(x => x.Customer)` / `Include(x => x.Items)` loads navigations through split query.
