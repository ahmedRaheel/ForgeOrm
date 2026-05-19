# ForgeORM Performance Patch - 12 Points Applied

This patch keeps NativeAOT/source-generation selection user configurable and adds the remaining performance refactoring items without creating separate `Fast*` public APIs.

## Added

1. Generated SQL cache per entity/provider: `ForgeGeneratedSqlCache`.
2. Prepared command shape cache: `ForgePerformanceCommandPlanCache`.
3. Provider-specific execution strategy selector:
   - SQL Server: SqlBulkCopy/TVP/MERGE strategy labels.
   - PostgreSQL: COPY/ON CONFLICT strategy labels.
   - MySQL: multi-row/ON DUPLICATE KEY strategy labels.
   - Oracle: array binding strategy labels.
4. Column ordinal result-shape cache: `ForgeColumnOrdinalShapeCache`.
5. Compiled projection cache: `ForgeProjectionPlanCache`.
6. Low-allocation pooled SQL builder: `ForgePooledSqlBuilder`.
7. Batch CRUD APIs on existing `db` surface: `InsertManyAsync`, `UpdateManyAsync`, `DeleteManyAsync`.
8. Transaction reuse context: `ForgeTransactionReuseContext` and `db.WithTransactionAsync`.
9. Compiled expression SQL plan cache: `ForgeExpressionSqlPlanCache`.
10. NativeAOT mode remains configurable through `ForgeCompilationConfiguration.ConfigureCompilation`.
11. Low-allocation diagnostics hook: `ForgePerformanceDiagnostics`.
12. Benchmark gate manifest and GitHub Actions workflow.

## Existing APIs remain the same

Normal methods continue to be the primary surface:

```csharp
db.QueryAsync<T>()
db.GetByIdAsync<T>()
db.InsertAsync<T>()
db.UpdateAsync<T>()
db.DeleteAsync<T>()
db.Set<T>().ToListAsync()
db.Set<T>().StreamAsync()
```

No additional `FastQuery`/`FastInsert` style round-trip APIs were introduced by this patch.

## NativeAOT configuration

NativeAOT is left to the user:

```csharp
ForgeCompilationConfiguration.ConfigureCompilation(options =>
{
    options.Mode = ForgeOrmCompilationMode.SourceGenerated;
});
```

JIT/runtime apps can use:

```csharp
ForgeCompilationConfiguration.ConfigureCompilation(options =>
{
    options.Mode = ForgeOrmCompilationMode.Auto;
});
```
