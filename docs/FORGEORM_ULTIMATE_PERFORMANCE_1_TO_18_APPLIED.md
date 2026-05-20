# ForgeORM Ultimate Performance 1-18 Patch

Applied as normal API infrastructure, not `Fast*` side APIs.

1. Generated provider-specific executor registry hooks added through `ForgeGeneratedExecutorRegistry` and SQL Server direct GetById executor.
2. SQL Server hot path continues to use `SqlConnection`, `SqlCommand`, `SqlDataReader`, and `SqlParameter`.
3. Include graph loader surface remains EF-style and `ThenInclude` compile-friendly overloads were restored.
4. SQL Server TVP batching helper added with scripts for `dbo.IntIdList`, `dbo.BigIntIdList`, and `dbo.GuidIdList`; expansion fallback remains when TVPs are not provisioned.
5. Generated/compiled projection-reader infrastructure hooks remain in `ForgeProjectionReaderCache` and provider-direct materializer cache.
6. Regex parameter scanning removed from SQL Server direct command plan extraction and replaced with a span-style scanner.
7. Pooled array primitive added for list/graph/bulk internals.
8. Generated enum converter primitive added to avoid repeated `Enum.Parse`/`Enum.ToObject` work in reusable converters.
9. Query hash key changed to a struct primitive for compiled plan registries.
10. NativeAOT mode config added: `options.UseNativeAotMode()`.
11. GetById SQL generation uses `SELECT TOP (1)` and explicit non-computed columns.
12. Relationship/key metadata now resolves `[ForgeKey]`, `[Key]`, `Id`, `<EntityName>Id`, and common `*Id` convention without requiring attributes.
13. Split query remains EF-style, not Dapper multi-mapping.
14. SQL parameter extraction avoids regex allocation.
15. DataFrame/vectorized/parallel features are preserved from previous patches.
16. Warmup and second-level cache features are preserved from previous patches.
17. Provider direct cache primitives were added without changing public APIs.
18. Benchmark gate coverage remains in `benchmarks/ForgeORM.Benchmarks` for Dapper/EF/ForgeORM comparisons.

Notes:
- For maximum split-query batching performance on SQL Server, run `ForgeSqlServerTvpBatching.CreateSqlServerTypesScript()` once on the database and route large key collections through TVP mode.
- I could not run `dotnet build` in this environment because the .NET SDK is not installed here.
