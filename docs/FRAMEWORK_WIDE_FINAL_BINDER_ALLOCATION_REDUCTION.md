# Framework-wide final binder allocation reduction

Applied to `ForgeDirectQueryExecutor` across Query, First, Single, Scalar, Execute, and Stream direct paths.

## Changes

- Replaced per-parameter getter arrays with a single compiled binder delegate per parameter object type.
- Precomputed parameter names, `SqlDbType`, and `DbType` during plan compilation instead of resolving type mappings on every execution.
- Scalar parameter plans now cache provider-neutral and SQL Server-specific parameter type metadata.
- Public API routing remains framework-level; provider-specific behavior stays internal to the direct executor.

## Goal

Reduce repeated hot-path binder overhead for anonymous parameters such as `new { Id = id }` and scalar parameters across all simple command shapes.
