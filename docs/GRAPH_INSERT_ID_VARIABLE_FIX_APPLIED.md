# Graph Insert Identity / @Id Fix Applied

Applied fixes:

- Graph insert resets numeric identity keys before insert.
- Graph insert excludes identity columns from insert SQL.
- Parent generated identity is written back after `SCOPE_IDENTITY()`.
- Child foreign keys are assigned from the generated parent identity.
- Child identities are reset before insert so SQL Server does not receive explicit identity values.
- Graph update insert-new-child path resets child identity before inserting.

This prevents:

```text
Cannot insert explicit value for identity column ...
Must declare the scalar variable "@Id"
```
