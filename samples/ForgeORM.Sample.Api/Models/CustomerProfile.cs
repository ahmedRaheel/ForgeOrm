using ForgeORM.Abstractions;
public sealed class CustomerProfile
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
}
