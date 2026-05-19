# ForgeORM graph insert and insert performance patch

Applied changes:

- Existing `Insert<T>` and `InsertAsync<T>` now use the cached fast insert pipeline internally.
- Fast insert uses scalar columns only, excludes identity columns, ignores navigations, and stores enums numerically using their underlying type.
- Graph insert helpers now never include numeric identity keys in INSERT statements.
- Graph child insert paths reset child identities before insert.
- Graph insert/update/delete continue to treat navigation properties as graph relationships, not SQL columns.

This fixes:

- `Cannot insert explicit value for identity column in table 'OrderItems' when IDENTITY_INSERT is set to OFF.`
- `Must declare the scalar variable "@Id"` caused by identity parameters being referenced after identity exclusion.
- Slow regular insert benchmarks that were still using the old provider insert path.
