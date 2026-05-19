# Graph Delete + Sample Endpoint Update Applied

## Core changes

Added `src/ForgeORM.Core/ForgeDb.GraphOptionOverloads.cs` with:

- `UpdateGraphAsync<T>(T entity, Action<ForgeGraphOptions> configure, CancellationToken)`
- `DeleteGraphAsync<T>(object id, Action<ForgeGraphOptions> configure, CancellationToken)`
- `DeleteGraphAsync<T>(T entity, Action<ForgeGraphOptions> configure, CancellationToken)`
- `DeleteGraphAsync<T>(T entity, CancellationToken)`
- hard-delete parent-only fallback
- soft-delete graph support using `ForgeGraphOptions.SoftDeleteColumn`

## Sample changes

Added separated endpoint module:

- `samples/ForgeORM.Sample.Api/Endpoints/GraphPersistenceEndpoints.cs`

Added sample routes:

- `POST /graph-persistence/orders/insert-auto`
- `POST /graph-persistence/orders/insert-tvp`
- `POST /graph-persistence/orders/insert-openjson`
- `PUT /graph-persistence/orders/update-with-children`
- `DELETE /graph-persistence/orders/{id:int}/hard`
- `DELETE /graph-persistence/orders/{id:int}/soft`

Updated `Program.cs` to call:

```csharp
app.MapGraphPersistenceEndpoints();
```

Also added `IsDeleted` to the sample `Order` and `OrderItem` models so soft delete examples match the sample schema.

## Notes

This environment does not include the .NET SDK, so `dotnet build` could not be executed here. The patch is static compile-focused and addresses the missing overload that caused the endpoint compilation problem.
