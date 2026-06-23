using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class OrderCreateRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "COD";

    [Required]
    [StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [MinLength(1)]
    public List<OrderItemCreateRequest> Items { get; set; } = new();
}

public class OrderItemCreateRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public class OrderStatusUpdateRequest
{
    [Required(ErrorMessage = "Vui lòng chọn trạng thái đơn hàng.")]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;
}

public class OrderResponse
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public List<OrderDetailResponse> Items { get; set; } = new();
}

public class OrderDetailResponse
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
