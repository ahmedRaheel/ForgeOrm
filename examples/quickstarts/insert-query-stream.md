# Insert, Query, Stream

```csharp
await db.InsertAsync(new Product { Name = "Keyboard", Price = 99.95m });

var products = await db.QueryAsync<Product>(
    "select Id, Name, Price from Products where Price >= @MinPrice",
    new { MinPrice = 50m });

await foreach (var product in db.StreamAsync<Product>("select Id, Name, Price from Products"))
{
    Console.WriteLine(product.Name);
}
```
