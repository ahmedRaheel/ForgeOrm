# ForgeORM User-Friendly API Pass

This pass adds easier APIs without removing the existing advanced APIs.

## QueryAst expression-first usage

```csharp
var rows = await ForgeSql
    .Select<Product>()
    .LeftJoin<Category>((p, c) => p.CategoryId == c.Id)
    .SelectColumns(p => p.Id, p => p.Code, p => p.Name, p => p.Price)
    .ColumnAs<Product, ProductListItem, string>(p => p.Name, x => x.Name)
    .Where(p => p.Price > minPrice)
    .OrderByDescending(p => p.Id)
    .Page(request.Page, request.PageSize)
    .ToListAsync<Product, ProductListItem>(db, ct);
```

## Split graph one-to-many

```csharp
var customers = await db
    .SplitGraph<Customer>()
    .Where(c => c.IsActive)
    .IncludeMany<Order>(
        parentKey: c => c.Id,
        childForeignKey: o => o.CustomerId,
        target: c => c.Orders)
    .ToListAsync(ct);
```

No more unclear `ids => sql` is required for the common case.
ForgeORM uses `@ParentIds` internally and still preserves old APIs.

## Entity with private backing field

```csharp
var order = await db
    .SplitGraph<Order>()
    .Where(o => o.Id == id)
    .IncludeMany<OrderItem>(
        parentKey: o => o.Id,
        childForeignKey: i => i.OrderId,
        backingField: "_items")
    .FirstOrDefaultAsync(ct);
```

## Many-to-many

```csharp
var products = await db
    .SplitGraph<Product>()
    .IncludeManyToMany<ProductTag, Tag>(
        parentKey: p => p.Id,
        joinParentKey: pt => pt.ProductId,
        joinChildKey: pt => pt.TagId,
        childKey: t => t.Id,
        assign: (product, tags) => product.Tags = tags.ToList())
    .ToListAsync(ct);
```

## Projection after graph load

```csharp
var result = await db
    .SplitGraph<Customer>()
    .IncludeMany<Order>(c => c.Id, o => o.CustomerId, c => c.Orders)
    .ToListAsync(c => new CustomerWithOrdersDto
    {
        Id = c.Id,
        Name = c.Name,
        Orders = c.Orders.Select(o => new OrderDto(o.Id, o.OrderNumber)).ToList()
    }, ct);
```

## Easy graph insert

```csharp
var key = await db.InsertGraphAsync(
    order,
    parent => parent.Items,
    child => child.OrderId,
    ct);
```

## DTO graph insert

```csharp
var key = await db.InsertGraphAsync<CreateOrderRequest, Order, CreateOrderItemRequest, OrderItem>(
    request,
    dto => dto.Items,
    child => child.OrderId,
    ct);
```

## Vector search SQL with expressions

```csharp
var sql = vectorSql.BuildPostgreSqlPgVectorSearch<DocumentEmbedding>(
    x => x.Id,
    x => x.Text,
    x => x.Vector,
    topK: 10);
```

## Graph persistence performance upgrade

Added a new provider-aware graph persistence foundation for fast EF-style graph operations without EF Core change tracking overhead.

### Added files

- `src/ForgeORM.Core/Graph/ForgeGraphResult.cs`
- `src/ForgeORM.Core/Graph/ForgeGraphResultBuilder.cs`
- `src/ForgeORM.Core/Graph/ForgeGraphIdentityMap.cs`
- `src/ForgeORM.Core/Graph/IForgeKeyResolver.cs`
- `src/ForgeORM.Core/Graph/ForgeKeyResolver.cs`
- `src/ForgeORM.Core/Graph/IForgeForeignKeyBinder.cs`
- `src/ForgeORM.Core/Graph/ForgeForeignKeyBinder.cs`
- `src/ForgeORM.Core/Graph/ForgeEntityMetadata.cs`
- `src/ForgeORM.Core/Graph/ForgeEntityMetadataCache.cs`
- `src/ForgeORM.Core/Graph/ForgeGraphPlan.cs`
- `src/ForgeORM.Core/Graph/ForgeGraphNode.cs`
- `src/ForgeORM.Core/Graph/ForgeGraphPlanBuilder.cs`
- Provider executor skeletons for SQL Server, PostgreSQL, MySQL, Oracle and SQLite.

### Strategy support

- SQL Server: OPENJSON, TVP, SqlBulkCopy, TempTable + MERGE
- PostgreSQL: jsonb_to_recordset, UNNEST, COPY, ON CONFLICT
- MySQL: multi-row INSERT, temp table, ON DUPLICATE KEY UPDATE
- Oracle: array binding, MERGE, global temporary table
- SQLite: transaction + prepared command batching

### New graph capabilities

- Identity map for generated key propagation.
- Convention-based parent key to child foreign-key binding.
- Cached metadata to reduce reflection overhead.
- Graph result statistics with inserted, updated, deleted, table counts, selected strategies and duration.
- Child sync modes: insert only, insert/update, insert/update/delete missing, replace all, ignore children.
- Soft delete and hard delete configuration.
