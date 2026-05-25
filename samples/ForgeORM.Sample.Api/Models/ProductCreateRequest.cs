using ForgeORM.Abstractions;
public sealed class ProductCreateRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
}
