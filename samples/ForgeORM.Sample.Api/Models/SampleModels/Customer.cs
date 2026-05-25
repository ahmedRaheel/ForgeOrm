using ForgeORM.Abstractions;

[ForgeTable("Customers")]
public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public CustomerProfile? Profile { get; set; }
    public List<Order> Orders { get; set; } = [];
}
