# ForgeORM Source-Generated Super Performance Engine Applied

This patch moves ForgeORM toward the ultimate high-performance ORM design while keeping the existing public API intact.

## Applied Design

### 1. SourceGenerated first, RuntimeEmit optional fallback

`ForgeOrmCompilationMode.Auto` now means:

1. Use source-generated readers/binders when registered.
2. Fall back to RuntimeEmit MSIL when source generation is unavailable.
3. User can still explicitly select `RuntimeEmit` or `SourceGenerated` from configuration.

```csharp
builder.Services.AddForgeOrm(options =>
{
    options.UseSqlServer(connectionString);
    options.UseCompilationMode(ForgeOrmCompilationMode.Auto);
});
```

NativeAOT is left to user configuration. For NativeAOT, choose:

```csharp
options.UseCompilationMode(ForgeOrmCompilationMode.SourceGenerated);
```

### 2. Record and constructor DTO support

The source generator no longer requires only parameterless constructors. It now supports:

```csharp
public sealed record OrderDto(int Id, string OrderNo, decimal GrandTotal);
```

Constructor parameters are matched by name to result columns/properties.

### 3. Column-name based ordinal binding

Generated readers bind ordinals once per result shape:

```text
CreateReader_Order(reader)
  -> ord_Id = Ordinal(reader, "Id")
  -> ord_OrderNo = Ordinal(reader, "OrderNo")
  -> returns row reader delegate
```

This avoids per-row `GetOrdinal` and prevents breakage when SQL column order changes.

### 4. Strong typed reader calls

Generated row readers use typed methods such as:

```csharp
reader.GetInt32(ord)
reader.GetDecimal(ord)
reader.GetString(ord)
reader.GetGuid(ord)
reader.GetFieldValue<T>(ord)
```

This avoids `GetValue`, boxing, `Convert.ChangeType`, and per-row reflection.

### 5. Generated binders

The generator emits per-type binders:

```text
Bind_Order(DbCommand, object)
```

This keeps parameter discovery out of the hot path and preserves the global scalar `@Id` fix.

### 6. Generated SQL builders

The generator emits basic SQL constants per type:

```text
SelectSql_Order
InsertSql_Order
UpdateSql_Order
DeleteSql_Order
```

These are the foundation for compile-time SQL builders.

### 7. Generated graph metadata and entity maps

The generator emits:

```text
GraphMetadata_Order
EntityMap_Order
```

This supports graph insert/update/delete planning without re-scanning attributes during execution.

### 8. Provider-specific concrete executor extension points

Provider packages now include concrete executor classes:

```text
SqlServerConcreteExecutor
PostgreSqlConcreteExecutor
MySqlConcreteExecutor
OracleConcreteExecutor
```

These preserve the public provider-neutral API while allowing provider-specific execution paths and future direct concrete-driver optimizations.

### 9. Registry lookup optimized

`ForgeSourceGeneratedRegistry` now caches type-to-provider resolution using `ConcurrentDictionary<Type, IForgeSourceGeneratedAccessorProvider?>`.

This removes repeated provider scans for hot-path calls.

## Performance Direction

The target architecture is now:

```text
Normal API call
  -> SourceGenerated registry
  -> generated reader/binder/sql/map
  -> provider-specific executor
  -> typed DbDataReader calls
  -> no reflection per row
  -> RuntimeEmit fallback only when configured/needed
```

## Important

RuntimeEmit is still available because dynamic DTOs, ad-hoc SQL, and unknown result shapes need fallback behavior. Production entities and DTOs should prefer source generation.
