# ForgeORM Provider Optimization Guide

## SQL Server

- Prefer `SqlBulkCopy` for large inserts.
- Prefer table-valued parameters for bulk update/delete/merge.
- Use native temporal syntax: `FOR SYSTEM_TIME AS OF`, `BETWEEN`, `ALL`.
- Use `DateTime2` and `DateTimeOffset` parameter normalization.
- Use seek pagination for large tables.

## PostgreSQL

- Prefer `COPY` for large imports.
- Use `ON CONFLICT` for merge/upsert.
- Use `jsonb` and vector extensions where available.
- Prefer keyset pagination over large offset pagination.

## MySQL

- Prefer multi-row insert for batches.
- Use `ON DUPLICATE KEY UPDATE` for upsert.
- Keep command batches under provider packet limits.

## Oracle

- Prefer array binding for batch operations.
- Use provider-specific parameter types for dates, decimals and CLOB/BLOB data.

## SQLite

- Use transaction-wrapped batches.
- Keep writes single-connection and short-lived.
