using ForgeORM.Abstractions;

public sealed record OrderSummaryRecord(int Id, string OrderNo, OrderStatus Status, decimal GrandTotal, DateTimeOffset CreatedAt);
