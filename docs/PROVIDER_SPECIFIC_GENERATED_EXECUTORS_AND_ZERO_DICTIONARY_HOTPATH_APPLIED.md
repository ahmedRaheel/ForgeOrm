# Provider-Specific Generated Executors + Low Allocation Hot Path

This update applies the final generated execution direction across the framework policy rather than one endpoint.

## Applied

- Added `IForgeNamedParameter` and `ForgeNamedParameter<T>` with `ForgeParameters.Id(id)` / `ForgeParameters.Of(name, value)`.
- Provider `BuildGetById` and `BuildDelete` methods now use typed named parameters instead of anonymous objects.
- Direct executor recognizes `IForgeNamedParameter` and binds the logical parameter directly.
- Source-generated registry now prefers provider-specific SQL Server generated executors when the connection is `SqlConnection`.
- Source generators emit SQL Server typed full-query executors using `SqlConnection`, `SqlCommand`, `SqlTransaction`, and the typed SQL Server reader path before falling back to provider-neutral `DbConnection` execution.
- Generated SQL Server list and first-row executors bind all SQL parameters through typed `SqlParameter` helpers.
- Provider-neutral generated executors remain as fallback for PostgreSQL/MySQL/Oracle until provider-specific packages generate their own typed executors.

## Execution order

```text
Query / First / Single / Scalar / Execute / Stream
  -> Framework policy
  -> Source-generated provider-specific executor when available
  -> Source-generated provider-neutral executor
  -> Direct runtime executor
  -> Compiled runtime fallback
```

## Hot-path parameter guidance

Prefer:

```csharp
ForgeParameters.Id(id)
ForgeParameters.Of("Status", status)
```

instead of:

```csharp
new { Id = id }
```

The old anonymous object style still works, but the typed named parameter avoids anonymous type reflection/accessor lookup.
