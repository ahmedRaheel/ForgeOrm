using ForgeORM.Abstractions;

public sealed record CustomerOrderAggregateDto(
    int CustomerId,
    int OrderCount,
    decimal TotalSales,
    decimal AverageOrderValue,
    decimal SmallestOrder,
    decimal LargestOrder);
