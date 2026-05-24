# Base Execution Performance Fix Applied

This patch changes the shared/base execution path instead of adding a single unused fast path.

## Fixed

- Removed sync-over-async from `ForgeFrameworkExecutionPolicy`.
- Removed sync-over-async from `ForgeDbConnectionExtensions`.
- Removed sync-over-async from core `ForgeAdo` sync APIs.
- Added real synchronous execution methods to `ForgePerformancePipeline`:
  - `Query<T>`
  - `FirstOrDefault<T>`
  - `SingleOrDefault<T>`
  - `Execute`
  - `ExecuteScalar<T>`
- Added real sync reader loops:
  - `ExecuteReaderList<T>`
  - `ExecuteReaderSingle<T>`

## Why this matters

The previous base path used:

```csharp
ValueTask.AsTask().GetAwaiter().GetResult()
```

That allocates a `Task`, blocks the async state machine, and keeps the benchmark on the expensive generic path.

