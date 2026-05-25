# Main Issue Fixed

The unchanged 17.37 KB allocation was not caused by source generation vs MSIL.
The real shared execution path still routed sync public APIs through async methods using:

```csharp
.AsTask().GetAwaiter().GetResult()
```

That creates Task/state-machine overhead and hides the real hot path behind async wrappers.

Fixed files:

- `src/ForgeORM.Core/Performance/ForgePerformancePipeline.cs`
  - Added true sync `Query`, `FirstOrDefault`, `SingleOrDefault`, `Execute`, `ExecuteScalar` methods.
  - Added true sync reader loops using `ExecuteReader` / `Read`.

- `src/ForgeORM.Core/Execution/ForgeFrameworkExecutionPolicy.cs`
  - Sync public gateway now calls true sync pipeline, not async bridge.

- `src/ForgeORM.Core/ForgeDbConnectionExtensions.cs`
  - Sync DbConnection extension methods now call true sync pipeline.
  - Removed duplicate `EnsureOpenAsync` declaration.

This targets the actual base execution layer instead of isolated fast-path experiments.
