namespace FrontendMvc.Models;

public class OrderAdminViewModel
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime OrderDate
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public List<OrderItemAdminViewModel> OrderItems { get; set; } = new();
    public List<OrderItemAdminViewModel> OrderDetails
    {
        get => OrderItems;
        set => OrderItems = value ?? new();
    }
}

public class OrderItemAdminViewModel
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal UnitPrice
    {
        get => Price;
        set => Price = value;
    }
    public decimal Total => Price * Quantity;
}
