using ForgeORM.Core;

public sealed class OrderSearchRequest
{
    public int CustomerId { get; set; }
    public string Status { get; set; } = "Paid";
}
