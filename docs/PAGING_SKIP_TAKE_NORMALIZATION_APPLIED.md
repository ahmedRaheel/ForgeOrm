# Paging Skip/Take Normalization Applied

Applied fixes:

- SQL Server/Oracle paging now always emits `ORDER BY 1` when paging is requested without explicit ordering.
- Paging values are normalized before rendering SQL.
- If `skip == take`, `take` is incremented by 1 as requested.
- `take <= 0` is normalized to 1.
- Fixed incorrect `PropertyInfo.GetValue()` usage inside navigation split query loading.

Updated areas:

- `ForgeORM.Core/Support.cs` expression query pipeline
- `ForgeORM.QueryAst/ForgeAstSelectBuilder.cs`
- `ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs`
- `ForgeORM.Core/QueryBuilder/ForgeQueryBuilder.cs`
- SQL Server / Oracle provider paging helpers where present
