# Compile fixes applied

- Added `ForgeORM.Core.SplitQuery` namespace.
- Added `db.SplitGraph<T>()` extension method.
- Added `db.SplitQuery<T>()` alias for read scenarios.
- Added compile-safe `ForgeSplitGraphBuilder<TParent>`.
- Added `IncludeOne`, `IncludeMany`, and `IncludeManyToMany` support matching the sample signatures.
- Fixed `/raw/orders/graph` endpoint to return an `IResult`.
- Added missing `using ForgeORM.Core.SplitQuery;` in the sample Program.cs.

Note: The sandbox does not have the .NET SDK installed, so this package was fixed by static source inspection rather than `dotnet build`.
