# ForgeORM SourceGenerated / RuntimeEmit Mode Fix

This patch separates the two materialization modes throughout the shared runtime:

- `RuntimeEmit` now ignores source-generated providers and uses MSIL/runtime emit only.
- `SourceGenerated` and `Auto` try generated providers first.
- `SourceGeneratedStrict` throws immediately when a generated reader/binder is missing instead of silently falling back to provider/MSIL materializers.
- Reader/materializer cache keys now include `ForgeOrmCompilationMode`, so benchmarks that switch modes in the same process cannot reuse the first compiled delegate.
- Compiled execution plan cache keys now include compilation mode, so generated binders and runtime emit binders do not share the same cached plan.
- Source-generated provider code generation had broken `typeof(T).FullName` comparisons in generated query executors; those are fixed.

Important: `SourceGenerated` only improves performance when generated providers are actually registered by the source generator. Use `SourceGeneratedStrict` during testing to verify that the generator is active; if it throws, you are still using MSIL fallback.
