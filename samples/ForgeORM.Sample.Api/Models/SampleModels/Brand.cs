using ForgeORM.Abstractions;

[ForgeTable("Brands")]
public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
