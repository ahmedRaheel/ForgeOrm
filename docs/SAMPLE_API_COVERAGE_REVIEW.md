# ForgeORM Sample API Coverage Review

This package updates the sample so each major API family has an endpoint category and compile-safe examples.

## Added / fixed in Core

- `db.Query<TEntity>()` zero-argument expression query builder.
- `ForgeQueryBuilder<TEntity>.From<TEntity>()`.
- `ForgeQueryBuilder<TEntity>.From(string)`.
- `Select(...)`, `Where(...)`, `WhereSql(...)`, `OrderBy(...)`, `OrderByDescending(...)`, `Skip(...)`, `Take(...)`, `Page(...)`, `Render()`, `ToSql()`, `ToListAsync(...)`.
- `db.SavedQueries.Register("Name", query => query.From<TEntity>()...)` lambda-root overload.
- `db.SavedQueries.Register<TEntity>("Name", query => ...)` typed lambda overload.
- Raw SQL saved query registration remains supported.
- Saved query list, execute, single-or-default, remove remain supported.

## Sample endpoint categories now covered

### Graph Persistence

Base route: `/graph-persistence`

Insert examples:

- Single entity insert.
- DTO insert.
- Insert many.
- Entity graph insert.
- DTO graph insert auto strategy.
- DTO graph insert TVP strategy.
- DTO graph insert OPENJSON strategy.
- Expression parent-child insert.
- DTO factory parent-child insert.
- Options-based parent-only graph insert.

Update examples:

- Single parent update.
- Graph update default.
- Graph update with delete missing children.
- Graph update insert/update child mode.
- Graph update insert/update/delete-missing child mode.
- Update by expression condition.
- Update by raw SQL condition.

Delete examples:

- Parent-only hard delete.
- Graph hard delete by id.
- Graph hard delete by entity.
- Graph soft delete by id.
- Graph soft delete by entity.
- Delete by expression condition.

### Saved Queries / Expression Query Builder

Base route: `/saved-queries`

- Register saved query using root lambda: `db.SavedQueries.Register("HighValueOrders", query => query.From<Order>()...)`.
- Register saved query using typed lambda: `db.SavedQueries.Register<Order>(...)`.
- Register saved query using raw SQL.
- Render SQL from `db.Query<Product>()`.
- Execute `db.Query<Product>()`.
- List saved queries.
- Execute saved query by name.
- Remove saved query.

### Reporting

Base route: `/reporting`

- Pivot SQL.
- Unpivot SQL.
- Window function SQL.
- Percentile SQL.
- Rolling average SQL.
- TopN SQL.
- DrillDown metadata.
- DrillThrough metadata.
- Export CSV.
- Export Excel XML.

## Existing Program.cs coverage retained

The original sample still includes examples for:

- Raw SQL query.
- Query single/single-or-default.
- Stored procedures.
- Scalar functions.
- QueryMultiple.
- String builder.
- AST builder.
- Joins.
- CTE.
- Temp table script.
- Pagination.
- Bulk insert.
- Transactions.
- Split query examples.
- Artifacts/view/procedure generation.
- Universal search.
- Record mapping.
- DTO insert.
- Graph insert TVP.
- Enum mapping.
- Cache.
- Security validation/masking.
- Telemetry.
- AI optimization/diagnostics/code generation/migrations.
- Vector search.
- Analytics window functions.
- DataFrame pivot/groupby/describe/import/export bridge.

## Note

The environment used to patch this package does not include the .NET SDK, so I could not execute `dotnet build`. The changes were made as static compile fixes based on the current source structure.
