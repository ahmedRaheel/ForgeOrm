# Graph Insert Identity and Scalar Column Fix Applied

Applied fixes:

- Graph insert excludes numeric identity keys such as `Id` from parent/child INSERT SQL.
- Child graph insert no longer sends explicit `OrderItems.Id` values when SQL Server IDENTITY is enabled.
- Graph row-by-row fallback, TVP DataTable creation, and automatic child collection insert use scalar insertable columns only.
- Graph update uses scalar update columns only and keeps key only in the WHERE parameter.
- Navigation properties such as `Customer` and `Items` are not treated as SQL columns.
- Default/invalid `DateTime` and `DateTimeOffset` values are normalized before database write.

This preserves EF-style behavior: the database generates identity values unless the key type is a client-generated key such as `Guid`.
