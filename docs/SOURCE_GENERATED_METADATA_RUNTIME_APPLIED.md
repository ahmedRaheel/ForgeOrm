# Source-generated metadata runtime applied

This patch converts metadata from reflection-only to a generated-first runtime model.

## Implemented

- Added `SourceGeneratedForgeEntityMetadataResolver`.
- Added `HybridForgeEntityMetadataResolver` for Auto mode.
- Added `ForgeSourceGeneratedRegistry.RegisterMetadata(...)` and `TryGetMetadata(...)`.
- Updated ASP.NET Core registration to use the hybrid resolver by default.
- Updated both source generator projects to emit `ForgeEntityMetadata` and `ForgePropertyMetadata` registration in the module initializer.

## Runtime behavior

```text
SourceGenerated mode:
  generated metadata required; missing metadata throws fast

Auto mode:
  generated metadata first; reflection fallback only if no generated metadata exists

RuntimeEmit mode:
  reflection fallback allowed
```

This makes the metadata path consistent with the reader/binder source generation path and removes reflection metadata discovery from generated entities.
