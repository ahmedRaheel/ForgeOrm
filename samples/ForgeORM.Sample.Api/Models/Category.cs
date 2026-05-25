using ForgeORM.Abstractions;
[ForgeTable("Categories")]
public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
