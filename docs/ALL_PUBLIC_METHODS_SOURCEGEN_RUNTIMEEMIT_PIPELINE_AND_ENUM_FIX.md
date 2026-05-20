# ForgeORM All Public Methods SourceGenerated/RuntimeEmit Pipeline + Enum SQL Fix

This patch applies the performance routing rule across the normal public surface:

- GetById
- Query list
- Insert single
- Insert many
- Bulk insert
- Graph insert
- Page
- Stream
- Split query
- Record DTO mapping
- Enum mapping

## Routing rule

Normal public APIs must keep their names and route internally as:

1. SourceGenerated compiled reader/binder/SQL if registered.
2. RuntimeEmit MSIL fallback if generated code is unavailable.
3. No reflection in the hot execution path.

## Enum SQL fix

The previous issue:

```text
Conversion failed when converting the nvarchar value 'Paid' to data type int.
```

was not a reader/materialization issue. SQL Server was comparing an `nvarchar` column containing values like `Paid` with an integer enum parameter. The parameter binder normalized enums to their numeric underlying type.

This patch changes default enum storage to string-name storage:

```csharp
OrderStatus.Paid -> "Paid"
```

Use numeric enum storage explicitly when the DB column is numeric:

```csharp
[ForgeEnumStorage(ForgeEnumStorage.Number)]
public OrderStatus Status { get; set; }
```

## Frame vectorized aggregate

This patch keeps/validates:

```csharp
var filtered = frame.Vectorized()
    .Where("GrandTotal", ForgeVectorOperator.GreaterThan, 10000m)
    .Sum("GrandTotal");
```

and expression frame aggregate:

```csharp
var revenue = await db.Frame<Order>()
    .Where(x => x.CreatedAt >= from)
    .Parallel()
    .MaxDegreeOfParallelism(8)
    .SumAsync(x => x.GrandTotal, ct);
```
