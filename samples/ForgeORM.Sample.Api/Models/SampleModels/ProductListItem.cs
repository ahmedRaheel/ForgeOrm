using ForgeORM.Abstractions;

public sealed record ProductListItem(int Id, string Code, string Name, decimal Price, string? CategoryName, string? BrandName);
