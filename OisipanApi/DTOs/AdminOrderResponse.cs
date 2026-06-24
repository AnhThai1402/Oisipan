namespace Oishipan.DTOs;

/// <summary>
/// DTO specifically designed for Admin Order viewing - includes all necessary fields
/// </summary>
public class AdminOrderResponse
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? VoucherCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? Notes { get; set; }
    public List<AdminOrderItemResponse> Items { get; set; } = new();
}

public class AdminOrderItemResponse
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
