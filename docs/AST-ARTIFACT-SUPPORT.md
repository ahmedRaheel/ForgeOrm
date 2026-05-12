# ForgeSql AST Artifact Support

## Create View

```csharp
var query = ForgeSql
    .Select<Product>()
    .Columns(x => x.Id, x => x.Name, x => x.Price)
    .Where(x => x.Price > minPrice);

var view = query
    .AsView("vw_ExpensiveProducts", schema: "dbo")
    .WithReason("Create typed AST view")
    .Render(db.Provider);

await artifactManager.CreateOrUpdateAsync(view.Artifact);
```

## Create Stored Procedure

```csharp
var procedure = query
    .AsProcedure("sp_GetExpensiveProducts", schema: "dbo")
    .WithParameter("@MinPrice", "DECIMAL(18,2)")
    .WithReason("Create procedure from typed AST")
    .Render(db.Provider);

await artifactManager.CreateOrUpdateAsync(procedure.Artifact);
```

## Automatic History

`ForgeArtifactManager` creates `ForgeOrmArtifactHistory` automatically in the same database.

It stores:

- Artifact type
- Schema
- Name
- Version
- SHA256 SQL hash
- SQL definition
- Previous hash/definition
- Applied user/machine/app
- Applied timestamp
