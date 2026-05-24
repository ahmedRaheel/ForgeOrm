# ForgeORM Performance Architecture Patch Applied

This patch moves ForgeORM closer to a Dapper-grade execution pipeline by wiring the missing architectural links rather than adding isolated micro-optimizations.

## Applied Changes

### 1. Typed source-generated reader path
`IForgeSourceGeneratedAccessorProvider` now supports:

```csharp
bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader, T>? readerFunc);
```

This lets generated providers return `Func<DbDataReader,T>` directly instead of wrapping `Func<DbDataReader,object>` and casting every row.

### 2. Backward compatible generated provider contract
The old methods remain:

```csharp
Func<DbDataReader, object> GetReader(Type type, DbDataReader reader);
Action<DbCommand, object> GetBinder(Type type);
```

Existing generated providers still work. New source-generated output uses the typed path.

### 3. Source generator emits typed reader switch
Both source generator projects now emit:

```csharp
public bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader,T>? readerFunc)
```

The generated switch casts once at factory time, not once per row.

### 4. Source-generated binder resolution
ForgeAdo now asks generated providers through:

```csharp
provider.TryGetBinder(type, out var generatedBinder)
```

This keeps generated binders as the first-class path before MSIL fallback.

### 5. Compiled reader resolver updated
`ForgeCompiledReaderResolver` now prefers the typed generated reader before falling back to MSIL.

Execution order is now:

```text
Source-generated typed reader
    ↓
MSIL DynamicMethod reader
    ↓
Object reader fallback
```

### 6. MSIL materializer cache updated
`ForgeIlMaterializerCache.GetOrCreate<T>` now also uses the typed generated reader path before emitting IL.

### 7. Hot parameter path hardened
`ForgeAdo` parameter extraction now uses a manual scanner instead of Regex + LINQ `Distinct` for command parameter tokens.

### 8. Command plan parameter extraction hardened
`ForgePerformanceCommandPlanCache` now uses loop-based parameter scanning instead of `HashSet`/LINQ-driven extraction.

### 9. Aggressive optimization attributes added
Hot-path methods now use:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[MethodImpl(MethodImplOptions.AggressiveOptimization)]
```

Applied to registry lookups, parameter binding, scalar detection, parameter creation, and command plan creation.

## New Runtime Flow

```text
User Query
   ↓
Create / fetch command plan
   ↓
Bind parameters using source-generated binder or MSIL binder
   ↓
Execute DbCommand
   ↓
Resolve source-generated typed materializer by reader shape
   ↓
Fallback to MSIL shape materializer
   ↓
Return list / stream
```

## Why This Helps Against Dapper

Dapper is fast because it avoids reflection in the hot path and emits/caches materializers. This patch makes ForgeORM follow the same core principles while keeping ForgeORM's higher-level advantages:

- graph operations
- typed SQL builder
- query analytics
- temporal queries
- source-generated plans
- provider optimization extension points
- NativeAOT-friendly source generation mode

## Important Note

The environment used for this patch does not have the .NET SDK installed, so the solution could not be compiled here. The patch was applied directly to source files and should be validated locally with:

```bash
dotnet restore
dotnet build ForgeORM.sln -c Release
```
