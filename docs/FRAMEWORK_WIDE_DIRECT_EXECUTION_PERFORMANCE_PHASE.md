# Framework-wide direct execution performance phase

This update keeps ForgeORM's single framework policy but makes the internal direct executor cheaper for every supported command mode:

- QueryAsync
- QueryFirstOrDefaultAsync / FirstAsync
- QuerySingleOrDefaultAsync / SingleAsync
- ExecuteScalarAsync
- ExecuteAsync

Changes applied:

1. Direct execution decision is cached as a `DirectExecutionPlan` keyed by SQL + parameter type + command type.
2. The cached plan owns the scalar parameter name or compiled parameter accessor.
3. The executor no longer rebuilds the same access decision and binder metadata separately for query/scalar/execute paths.
4. Added optional `ForgeDirectExecutionDiagnostics` counters to prove whether the direct path is being hit during benchmarks.
5. Fixed the source generator emitted-character bug that duplicated `var start` in `ParseFirstParameterName`.

Benchmark verification:

```csharp
ForgeDirectExecutionDiagnostics.Enabled = true;
ForgeDirectExecutionDiagnostics.Reset();

await forge.GetByIdAsync(1);

Console.WriteLine(ForgeDirectExecutionDiagnostics.FirstHits);
Console.WriteLine(ForgeDirectExecutionDiagnostics.FallbackRejects);
```

Keep diagnostics disabled for normal benchmark runs after verifying path usage.
