# ForgeORM Allocation Reduction Patch Applied

This patch targets the benchmark gap where ForgeORM showed 3 hot-path allocations compared with Dapper 1 and EF 2.

Applied changes:

1. `ForgePerformancePipeline` query/scalar/page methods now return `ValueTask` to avoid `Task` allocation on synchronous/cached completions.
2. Added typed `QueryAsync<T, TParameters>` overload so new public APIs can avoid boxing parameter containers.
3. Added `ForgeCommandParameterLayout` to prepare cached parameter names once per command and let binders assign values instead of re-discovering parameter names.
4. Fallback binder now uses `SetOrAdd` parameter behavior, so it updates existing cached-layout parameters instead of blindly adding duplicates.
5. Reader-shape cache no longer uses `string.Join` over column arrays; it uses the existing provider/type/column/type/nullability fingerprint.
6. Result lists remain pre-sized using `EstimateCapacity` to avoid first-resize allocations for common small result sets.

Important next benchmark target:

- Use the new typed overloads from all public APIs that know their parameter type.
- Update generated binders to set existing command parameters by ordinal/name instead of always calling `CreateParameter`.
- Add optional prepared command pooling for repeated same-connection queries if allocation count must match Dapper exactly.
