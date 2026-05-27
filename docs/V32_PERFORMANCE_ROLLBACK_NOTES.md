# ForgeORM v32 Performance Rollback Notes

This build reverts the v29 SQL Server latency hot-path changes that caused Query_By_Id allocation to jump to ~48 KB.

Kept:
- RuntimeEmit/MSIL materializer path
- enum string parameter binding fix
- Dapper-level allocation path from v28/v31 core
- DataFrame/Pandas analytics API additions from v31

Removed/Reverted:
- v29 direct hot-path changes that introduced excessive allocations
- typed scalar overload routing that created extra plan/parameter wrapper allocations
- altered command behavior routing that pushed queries through a heavier path

Expected baseline target:
- Query_By_Id allocation back near ~7.84 KB
- Search_Paged allocation near Dapper-level numbers

Recommendation:
- Treat v28/v31 core as the performance freeze baseline.
- Future latency work must be profiler-driven and benchmarked one change at a time.
