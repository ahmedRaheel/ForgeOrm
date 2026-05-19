# System-wide MSIL/DataReader and Graph SQL Fix Applied

Applied to ForgeORM.Core:

- Existing typed query methods route through ForgeAdo DbDataReader execution.
- DbConnection extension single-row methods no longer allocate lists.
- Cached MSIL materializer remains the universal typed materialization path.
- Cached MSIL parameter writer is used for anonymous/POCO parameter bags.
- SQL parameter token extraction is cached in ConcurrentDictionary and null-safe.
- Graph insert SQL is built from scalar non-identity properties before SQL generation.
- Numeric database generated Id columns are excluded from graph insert SQL and parameter dictionaries.
- Graph delete parent command has @Id parameter fallback.
