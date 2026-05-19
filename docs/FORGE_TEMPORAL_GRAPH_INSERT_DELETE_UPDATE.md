# ForgeORM Graph Insert/Delete + Temporal Table Update

This update adds focused support for:

## Graph insert/delete fixes

- Graph insert excludes database-generated numeric identity keys from INSERT SQL.
- Graph insert reads `SCOPE_IDENTITY()` and writes the generated key back to the entity.
- Child graph insert receives the generated parent key before insertion.
- Graph delete deletes children first, then parent, and explicitly binds both `@Id` and the key property name to avoid missing scalar variable errors.
- Graph operations use scalar properties only. Navigation properties are never written as table columns.

## Temporal table support

Added:

```csharp
[ForgeTemporal]
[ForgeTable("Orders")]
public sealed class Order { }
```

Core API:

```csharp
await db.TemporalAllAsync<Order>();
await db.TemporalAsOfAsync<Order>(pointInTimeUtc);
await db.TemporalBetweenAsync<Order>(fromUtc, toUtc);
await db.TemporalContainedInAsync<Order>(fromUtc, toUtc);
await db.TemporalAsOfByIdAsync<Order>(id, pointInTimeUtc);
```

Expression API:

```csharp
await db.Set<Order>()
    .TemporalAsOf(pointInTimeUtc)
    .Where(x => x.Id == id)
    .FirstOrDefaultAsync();
```

ForgeSQL / QueryAst API:

```csharp
ForgeSql.Select<Order>()
    .TemporalAll()
    .Where(x => x.Id == id)
    .Render(provider);
```

SQL generation helpers:

```csharp
var sql = db.GenerateEnableTemporalSql<Order>();
var disable = db.GenerateDisableTemporalSql<Order>();
```

Temporal support is opt-in and does not affect normal query performance.
