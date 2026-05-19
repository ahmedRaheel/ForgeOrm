# ForgeORM Stable API Surface

Stable public API families:

- `QueryAsync<T>`, `QueryFirstAsync<T>`, `QuerySingleAsync<T>`
- `ExecuteAsync`, `ExecuteScalarAsync<T>`
- `StreamAsync<T>`
- `PageAsync<T>` and seek paging helpers
- `InsertAsync<T>`, `UpdateAsync<T>`, `DeleteAsync<T>`
- `InsertGraphAsync<T>`, `UpdateGraphAsync<T>`, `DeleteGraphAsync<T>`
- `BulkInsertAsync<T>`, `BulkUpdateAsync<T>`, `BulkDeleteAsync<T>`, `BulkMergeAsync<T>`
- Temporal helpers: `AsOf`, `Between`, `History`, `All`
- QueryAst methods: `Where`, `OrderBy`, `GroupBy`, `Having`, `Union`, `Cte`, `TempTable`, `Pivot`
- AI/vector/dataframe helpers as optional packages

Compatibility rule: do not break these method names or parameter order after 1.x. Add overloads instead.
