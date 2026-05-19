# ForgeORM 18 Patch

Applied to the uploaded solution:

- `ForgeAdo.QueryAsync<T>`, `QueryFirstOrDefaultAsync<T>`, and `QuerySingleOrDefaultAsync<T>` use `DbDataReader` with `ForgeIlMaterializerCache`.
- Single-row methods do not route through `QueryAsync<T>` / `List<T>`.
- Parameter binding is null-safe and supports anonymous objects, dictionaries, and enumerable parameters.
- `EnsureReferencedSqlParametersAreBound` no longer throws `NullReferenceException` during graph delete/update when parameters are null or dictionary-based.
- Graph dynamic query reader uses `CommandBehavior.SequentialAccess`.
- Graph insert existing identity reset / generated identity assignment flow is preserved.
- Existing public methods remain the entry points; no benchmark-only query APIs are required.

Important: dynamic dictionary queries still return dictionaries by design; typed queries use the MSIL materializer.
