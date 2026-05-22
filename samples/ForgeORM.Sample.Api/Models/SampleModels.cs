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

[ForgeTable("Categories")]
public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

[ForgeTable("Brands")]
public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

[ForgeTable("Customers")]
public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public CustomerProfile? Profile { get; set; }
    public List<Order> Orders { get; set; } = [];
}

public sealed class CustomerProfile
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
}

[ForgeTable("Orders")]
public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string OrderNo { get; set; } = "";
    [ForgeEnumStorage(ForgeEnumStorage.String)]
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal GrandTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

[ForgeTable("OrderItems")]
public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class ProductCategory
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
}

public sealed record ProductListItem(int Id, string Code, string Name, decimal Price, string? CategoryName, string? BrandName);

public sealed record OrderSummaryRecord(int Id, string OrderNo, OrderStatus Status, decimal GrandTotal, DateTimeOffset CreatedAt);

public sealed record CustomerOrderAggregateDto(
    int CustomerId,
    int OrderCount,
    decimal TotalSales,
    decimal AverageOrderValue,
    decimal SmallestOrder,
    decimal LargestOrder);

public enum OrderStatus
{
    Draft = 0,
    Pending = 1,
    Paid = 2,
    Shipped = 3,
    Completed = 4,
    Cancelled = 5,
    Processing = 6,
}

public sealed class ProductCreateRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
}

public sealed class CreateOrderRequest
{
    public int CustomerId { get; set; }
    public string OrderNo { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public decimal GrandTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public sealed class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public sealed class NdpStatement
{
    [ForgeColumn("current_financial_stat_year")]
    public int CurrentFinancialStatYear { get; set; }

    [ForgeColumn("EBITDA_To_Total_Indebtedness")]
    public decimal? EbitdaToTotalIndebtedness { get; set; }

    public string Sector { get; set; } = string.Empty;
}
