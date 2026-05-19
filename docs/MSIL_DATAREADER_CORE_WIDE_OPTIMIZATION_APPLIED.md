# MSIL + DataReader Core-Wide Optimization Applied

This package updates the existing ForgeORM query execution pipeline instead of adding benchmark-only APIs.

## Applied changes

- Existing `ForgeAdo.QueryAsync<T>` uses `DbDataReader` + cached MSIL materializer.
- Existing `ForgeAdo.QueryFirstOrDefaultAsync<T>` uses direct single-row reader execution and does not allocate `List<T>`.
- Existing `ForgeAdo.QuerySingleOrDefaultAsync<T>` reads at most two rows and uses cached MSIL materializer.
- Added `CommandBehavior.SequentialAccess` and `SingleRow | SequentialAccess` where applicable.
- Replaced expression-based anonymous parameter getters with a cached MSIL parameter writer per parameter object type.
- Added `ConcurrentDictionary<Type, Action<DbCommand, object>>` parameter writer cache.
- Materializer cache uses `ConcurrentDictionary<string, Delegate>` per entity/result-shape.
- Enum parameters are normalized to their numeric underlying value.
- Enum materialization reads numeric underlying values via `DbDataReader.GetFieldValue<TUnderlying>()`.
- DateTime and DateTimeOffset parameter safety remains.

## Goal

This moves ForgeORM closer to Dapper's approach:

`DbDataReader -> cached DynamicMethod/MSIL -> entity`

and avoids reflection/materialization overhead across all existing query methods.
