using ForgeORM.Abstractions;

[ForgeTable("Orders")]
public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string OrderNo { get; set; } = "";
      public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal GrandTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
