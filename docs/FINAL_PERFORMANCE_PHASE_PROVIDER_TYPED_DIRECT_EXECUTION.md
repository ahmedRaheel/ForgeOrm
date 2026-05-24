# Final Performance Phase: Provider-Typed Direct Execution

This update keeps ForgeORM's framework-level execution policy intact while reducing overhead across all direct hot paths.

## Applied globally

- Query, First/Single, Scalar, Execute and Stream continue to enter through `ForgePerformancePipeline`.
- The pipeline now uses `Try*` direct-executor calls, avoiding the previous double `CanUse()` + `GetPlan()` lookup.
- Direct execution plans are cached once per `(SQL, parameter type, command type)`.
- Parameter names are normalized once during plan/accessor creation, not on every bind.
- SQL Server uses `SqlCommand.Parameters.Add(name, SqlDbType)` on the direct path.
- Provider-neutral fallback remains for PostgreSQL/MySQL/Oracle/other ADO.NET providers.
- Warmup now primes the direct executor before falling back to the compiled execution plan cache.

## Why this is framework-level

There is still one public execution framework:

`ForgeDb / DbContext / builder APIs -> ForgePerformancePipeline -> Direct plan if safe -> compiled fallback if complex`

Provider-specific behavior is internal only. SQL Server gets a typed parameter fast path, while other providers keep the generic `DbCommand` implementation.
