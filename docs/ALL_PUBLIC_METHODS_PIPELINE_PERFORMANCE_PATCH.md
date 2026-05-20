# ForgeORM All Public Methods SourceGenerated/RuntimeEmit Pipeline Performance Patch

This patch routes the central public query materialization paths through `ForgeCompiledReaderResolver`:

- `QueryAsync<T>`
- `QueryFirstOrDefaultAsync<T>`
- `QuerySingleOrDefaultAsync<T>`
- streaming through `ForgePerformancePipeline.StreamAsync<T>`
- `GetById/GetByIdAsync` now uses the single-row path directly instead of materializing a list.

Resolution order:

1. SourceGenerated reader/binder when registered and mode is `Auto` or `SourceGenerated`.
2. RuntimeEmit MSIL fallback when mode is `Auto` or `RuntimeEmit`.
3. No public `Fast*` API is required by callers. Existing public methods use the optimized path.

Frame fix:

```csharp
var filtered = frame.Vectorized()
    .Where("GrandTotal", ForgeVectorOperator.GreaterThan, 10000m)
    .Sum("GrandTotal");
```

Enum fix:

Source generated readers now route enums through generated `ReadEnum<TEnum>` helpers, allowing both string enum storage (`Paid`) and numeric enum storage.

Build note: the patch was applied in the artifact environment. Run `dotnet clean && dotnet restore && dotnet build -c Release` locally because the environment has no .NET SDK.
