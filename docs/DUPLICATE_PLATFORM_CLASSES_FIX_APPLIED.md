# Duplicate platform classes fixed

Applied fixes:

- Renamed Core runtime-only `ForgeTenantContext` to `ForgeRuntimeTenantContext`.
- Renamed Core runtime-only `ForgeOutboxMessage` to `ForgeRuntimeOutboxMessage`.
- Updated enterprise runtime extension references.
- Updated `ForgePlatform` service implementations to explicitly use `ForgeORM.Abstractions.ForgeTenantContext` and `ForgeORM.Abstractions.ForgeOutboxMessage`.
- Added `ForgeDb.Analysis` facade so sample code can call `db.Analysis.Analyze(sql)` or `db.Analysis.Analyze(query)`.

Reason: the core runtime records conflicted with abstraction contract records used by `IForgeTenantProvider` and `IForgeOutboxStore`.
