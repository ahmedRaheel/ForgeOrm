# ForgeORM Complete ValueTask Migration

This package migrates ForgeORM hot-path async APIs from `Task` / `Task<T>` to `ValueTask` / `ValueTask<T>` across the source tree.

## Applied

- Replaced public/internal async ORM API return types from `Task<T>` to `ValueTask<T>`.
- Replaced non-generic ORM async methods from `Task` to `ValueTask`.
- Updated core interfaces in `ForgeORM.Abstractions`.
- Updated provider contracts and provider implementations.
- Updated query, scalar, execute, bulk, transaction, graph, split-query, analytics, caching, AI, RAG, sync and workflow async surfaces.
- Replaced `Task.FromResult(...)` with `ValueTask.FromResult(...)`.
- Replaced `Task.CompletedTask` with `ValueTask.CompletedTask`.
- Updated callback delegates such as `Func<IReadOnlyList<T>, ValueTask>`.
- Added `ForgeRuntimeMemberCache.AwaitAndGetResultAsync(...)` so reflection-based fallback can handle both `Task<T>` and `ValueTask<T>`.

## Intentionally Kept

Some `Task` usages remain where they are runtime primitives, not API return types:

- `Task.Delay(...)`
- `Task.Yield()`
- `Task.WhenAll(...)`
- internal reflection support for awaiting legacy `Task` return values

These should not be converted blindly.

## Important

Build was not run in this environment because the .NET SDK is unavailable. Run:

```bash
dotnet clean
dotnet build
```

Then fix any project-specific compatibility errors where external interfaces still require `Task`.
