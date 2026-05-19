# ForgeORM MSIL Materializer Fast Path Applied

This update adds a Dapper-style materialization path using `System.Reflection.Emit.DynamicMethod` and `ILGenerator`.

## Included

- `ForgeIlMaterializerCache`
- ConcurrentDictionary cache per entity/result-shape
- Existing `ForgeAdo.QueryAsync<T>` uses cached MSIL materializer once per result set
- Existing `ForgeAdo.QueryFirstOrDefaultAsync<T>` uses cached MSIL materializer
- Existing `ForgeAdo.QuerySingleOrDefaultAsync<T>` uses cached MSIL materializer
- Enum storage default changed to numeric underlying type
- Navigation properties remain excluded from scalar materialization

## Notes

This improves existing methods; users do not need to call new fast APIs.
