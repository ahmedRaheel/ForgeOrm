# Span/ArrayPool Execution Engine Cleanup Applied

This patch removes avoidable allocations from ForgeORM hot execution infrastructure without creating a separate framework path.

Applied changes:

1. Removed LINQ hot-path usage from SQL Server parameter binder and SQL Server direct materializer construction.
2. Replaced parameter key `OrderBy + string.Join` with one cached manual key builder.
3. Replaced materializer cache key `string[] + string.Join` with one pre-sized `StringBuilder` pass.
4. Replaced reader constructor ordinal map `Enumerable.Range().ToDictionary()` with manual dictionary construction.
5. Replaced split-query `Select/Distinct/GroupBy/ToDictionary/Select.ToList` flows with loop-based dictionaries/lists.
6. Replaced query-ast `WhereIds` LINQ chain with manual distinct key collection.
7. Replaced collection parameter expansion name-list allocation with `ArrayPool<string>` and span-based parameter-list rendering.
8. Kept provider-native behavior intact: PostgreSQL ANY, expanded parameters for SQL Server/MySQL/SQLite fallback.

Expected impact:

- Lower allocation in split-query and graph include paths.
- Less GC pressure during parameter expansion and materializer construction.
- More stable warm-path execution by removing LINQ iterator/dictionary churn.
- Single-row ADO.NET baseline allocations still remain because `DbCommand`, `DbParameter`, and provider internals are unavoidable unless command pooling/provider-specific generated executors are used.
