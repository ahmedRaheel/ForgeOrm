# Framework-wide generated execution priority applied

ForgeORM now tries complete source-generated query executors before the generic Dapper-style direct executor and before the compiled runtime fallback.

Applied globally to:

- QueryAsync
- QueryAsync<T,TParameters>
- FirstOrDefaultAsync
- FirstOrDefaultAsync<T,TParameters>
- SingleOrDefaultAsync
- ExecuteScalarAsync
- ExecuteAsync

Final framework order:

1. Source-generated whole-query executor when available and enterprise runtime is disabled.
2. Dapper-style direct executor for safe simple command shapes.
3. Compiled runtime execution plan fallback.

This keeps one framework policy while allowing the fastest generated path to dominate hot queries.
