# Zero Reflection MSIL Hot Path Patch Applied

This patch routes the existing ForgeORM APIs through cached MSIL delegates instead of adding separate Fast* methods.

## Fixed

- `db.GetByIdAsync<OrderDto>(id)` now guarantees `@Id` is bound even when the provider returns an anonymous object with an `object`-typed Id value.
- The parameter binder now treats `object` parameter properties as bindable, fixing `Must declare the scalar variable "@Id"` in GetById, Delete, Graph Insert, and Graph Update style flows.

## Implemented

- `ForgeIlAccessors`
  - MSIL getter cache
  - MSIL setter cache
  - MSIL constructor cache
  - MSIL list factory cache
  - ConcurrentDictionary-backed property accessor plans
  - ConcurrentDictionary-backed default value cache

- `ForgeAdo`
  - Existing Query/Execute/Scalar APIs use the cached MSIL parameter binder.
  - Removed `Expression.Compile` from parameter getter creation.
  - Removed provider reflection from the hot AddParameter path.
  - Added `EnsureIdParameter` safety for `@Id` across GetById/Delete paths.

- `ForgeMaterializer`
  - Existing materialization now delegates to `ForgeIlMaterializerCache`.
  - Removed Activator/PropertyInfo setter fallback from the materialization hot path.

- Graph operations
  - Graph insert/update/delete now use `ForgeIlAccessors.Get/Set` for keys, foreign keys, and collection assignment.
  - Child collection and foreign key discovery are cached with ConcurrentDictionary.
  - Graph parameter dictionary creation uses cached MSIL getters.

## Current rule

Reflection remains only for one-time metadata/plan construction. Runtime execution uses cached delegates.
