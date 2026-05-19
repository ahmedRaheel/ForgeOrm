# Query Fast Path and Graph Scalar Patch Applied

Applied changes:

- `FirstOrDefault` / `FirstOrDefaultAsync` now execute a direct single-row query using `TOP 1` instead of mutating `Take(1)` and routing through list/paging SQL.
- Expression SQL, count SQL, any SQL, aggregate SQL and list SQL are cached per query shape.
- Expression translator caches predicate and member-name translation.
- Materializer now caches type plans and result-set column plans, reducing per-row column/property matching overhead.
- ADO parameter binding now uses cached compiled parameter getters and keeps navigation properties out of parameters.
- Default `DateTime` and `DateTimeOffset` values are normalized before binding.
- Graph entity shape now uses scalar-only properties so `Customer`, `Items`, and other navigations are not treated as database columns in graph insert/update/delete.

Notes:

- Default queries remain parent-only.
- `Include(x => x.Items)` and `Include(x => x.Customer)` still load navigations by split query.
- .NET SDK was not available in this environment, so run `dotnet build` locally after download.
