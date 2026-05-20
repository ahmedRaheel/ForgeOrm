# ForgeORM source-generator completeness + provider-native bulk patch

Applied areas:

## Source generator completeness

- class, record, and struct candidate discovery
- constructor/record DTO mapping by normalized parameter/column/property name
- nullable scalar support
- enum materialization from string or numeric database values
- enum binding as string by default to match nvarchar enum columns
- composite key metadata via repeated `[ForgeKey]`/`[Key]` and `Order` named argument
- key conventions: `[ForgeKey]`, `[Key]`, `Id`, `<EntityName>Id`
- generated SQL constants for select/insert/update/delete/get-by-id
- generated entity map, projection metadata, and relationship metadata
- normalized column aliases: `customer_id`, `CustomerId`, `CustomerID`, `customer-id`

## Provider-native bulk

- SQL Server bulk insert path uses `SqlBulkCopy` when a `SqlConnection` is supplied
- SQL Server bulk update/merge foundation remains `TVP + MERGE` compatible through provider strategy metadata
- PostgreSQL provider now has a native bulk hook file for COPY-oriented implementation points
- MySQL provider now has a native multi-row insert hook file
- Oracle provider now has a native array-binding hook file

The native bulk hooks keep the public `BulkInsertAsync` API unchanged while allowing provider packages to bypass row-by-row fallback paths.
