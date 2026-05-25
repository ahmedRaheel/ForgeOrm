using ForgeORM.Abstractions;
[ForgeTable("Products")]
public sealed class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public List<Category> Categories { get; set; } = [];
}
