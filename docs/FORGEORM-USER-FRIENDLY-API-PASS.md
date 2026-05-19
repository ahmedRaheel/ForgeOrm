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
