# ForgeORM Enterprise Enum + Architecture Upgrade Applied

This patch removes the need for `[ForgeEnumStorage]` during enum materialization.

## Enum behavior

Reader/materializer behavior is now storage agnostic:

- `INT`, `BIGINT`, `SMALLINT`, etc. -> enum via `Enum.ToObject`
- `VARCHAR` / `NVARCHAR` enum names like `Paid` -> enum via case-insensitive parse
- numeric text like `'2'` -> enum numeric conversion
- nullable enum columns are supported

Binder/write behavior remains enterprise-safe and EF-style by default:

- enum parameters are sent as numeric values by default
- explicit string enum storage is still possible, but not required for reading

## Raw SQL safety

Raw SQL parameter binding also normalizes enum string parameters when the target result type contains a matching enum property.
A defensive literal normalizer handles simple legacy raw SQL patterns such as:

```sql
WHERE Status = 'Paid'
```

and rewrites it to the numeric enum value when the target model has an enum `Status` property.

## Source generation improvement

Generated readers now use the actual target type in `ForgeColumnOrdinalShapeCache` instead of `typeof(object)`.
This avoids accidental ordinal-map reuse across different result types with similar reader shapes.

## Sample cleanup

The sample `OrderSummaryRecord` no longer requires `[ForgeEnumStorage]`.
