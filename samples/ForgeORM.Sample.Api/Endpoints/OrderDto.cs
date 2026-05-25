using System.Buffers;
using ForgeORM.Core;
using ForgeORM.Core.Search;
using ForgeORM.DataFrame;
public sealed class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
