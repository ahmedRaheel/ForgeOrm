# Provider Materializer Accessibility Fix

Fixed provider compilation issue where provider projects such as `ForgeORM.Providers.MySql` could not access `ForgeMaterializer` because it was declared `internal` in `ForgeORM.Core`.

## Change

`ForgeMaterializer` is now public so provider assemblies can use the shared optimized materialization pipeline:

```csharp
public static class ForgeMaterializer
{
    public static Func<DbDataReader, T> GetReader<T>(DbDataReader reader);
    public static Func<DbDataReader, object> GetReader(Type type, DbDataReader reader);
    public static T Map<T>(DbDataReader reader);
    public static object? Map(Type type, DbDataReader reader);
    public static bool IsScalar(Type type);
}
```

## Why

Provider projects are separate assemblies. `internal` members are visible only inside the defining assembly unless `InternalsVisibleTo` is used. Since provider executors intentionally share the same compiled materialization pipeline, this type must be accessible from provider assemblies.

## Affected providers

- `ForgeORM.Providers.MySql`
- `ForgeORM.Providers.PostgreSql`
- `ForgeORM.Providers.SqlServer`
- `ForgeORM.Providers.Oracle`

No features were removed.
