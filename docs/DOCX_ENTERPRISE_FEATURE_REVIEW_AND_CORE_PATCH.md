# ForgeORM DOCX Enterprise Feature Review + Core Patch

The uploaded document lists a large strategic roadmap. This patch implements the practical/core items that affect the current ORM runtime without introducing external services or fake implementations.

Implemented in this package:

- Shared `ForgeQueryExecutionOptions` and `IForgeExecutableQuery`.
- Lock/read-consistency/query-tag/timeout extension methods.
- Central SQL Server lock hint rendering for expression queries.
- Expression query `.ToSql()`.
- Expression query streaming and batch processing APIs.
- Lightweight query monitor primitives.
- Cached SQL parameter token extraction.
- Retained universal `DbDataReader + MSIL + ConcurrentDictionary` typed query path.
- Hardened graph insert SQL generation so database-generated identity keys are not emitted as `@Id`.
- Existing temporal methods retained.

Not implemented as real production modules in this patch because they require separate services/providers/infrastructure:

- Full OLAP cube engine.
- Real ML forecasting/anomaly/clustering engine.
- Full built-in Elasticsearch-class search engine.
- Real distributed consensus/leader election.
- Complete visual Studio designer.
- Full multi-provider production parity for PostgreSQL/MySQL/Oracle.

Those should be delivered as separate packages/modules after the core execution engine is stable.
