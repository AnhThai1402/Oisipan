using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Oishipan.DTOs;

public class OrderCreateRequest
{
    public Guid UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "COD";

    [Required]
    [StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [MinLength(1)]
    public List<OrderItemCreateRequest> Items { get; set; } = new();

    public string? VoucherCode { get; set; }
}

public class OrderItemCreateRequest
{
    public Guid ProductId { get; set; }

    [Range(1, byte.MaxValue)]
    public byte Quantity { get; set; }

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
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public Guid? VoucherId { get; set; }
    public string? VoucherCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public List<OrderDetailResponse> Items { get; set; } = new();
    public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = new();
    public string RefundStatus { get; set; } = "NotRequired";
    public DateTime? RefundedAt { get; set; }
}

public class OrderStatusHistoryResponse
{
    public Guid OrderStatusHistoryId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class OrderRefundRequest
{
    [StringLength(500)]
    public string? Note { get; set; }
}

public class OrderDetailResponse
{
    public Guid OrderDetailId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public byte Quantity { get; set; }
    public string? Note { get; set; }
}

public class OrderCancellationDto
{
    public Guid OrderCancellationId { get; set; }
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CancelledBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
    public DateTime? ResponseDate { get; set; }
    public string? AdminNote { get; set; }
}

public class OrderCancellationCreateDto
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
