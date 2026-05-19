# Query Parameter Binding Fix Applied

All existing query entry points still route through `ForgeAdo.CreateCommand`, but parameter binding now has a shared safety check:

- anonymous objects like `new { Id = id }` bind correctly
- dictionary parameters bind correctly
- SQL tokens such as `@Id` are verified before command execution
- missing SQL parameters are rebound from the cached parameter property map
- existing MSIL parameter writer remains the primary fast path

This fixes `Must declare the scalar variable "@Id"` for `GetByIdAsync<T>()`, `QuerySingleOrDefaultAsync<T>()`, and other query methods.
