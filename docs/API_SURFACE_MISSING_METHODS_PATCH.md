# API Surface Missing Methods Patch

Added and wired missing examples/APIs:

- `options.UseCompilationMode(ForgeOrmCompilationMode.Auto)`
- `db.AI.OptimizeAsync(...)`
- `db.Search<T>().FullText(...).Fuzzy().Top(...).ToListAsync(...)`
- `db.Cte<T>().With(...).From(...).ToListAsync(...)`
- `db.TempTable<T>(name).FromQuery(...).CreateAsync(...)`
- `db.QueryAsync<T>(sql, CancellationToken)` overload for sample-friendly calls

The normal db-level API remains unchanged; no `Fast*` APIs were added.
