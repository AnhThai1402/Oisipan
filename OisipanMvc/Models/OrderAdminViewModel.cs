using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;

public class OrderAdminViewModel
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }

    [Display(Name = "Tên khách hàng")]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Email khách hàng")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Chờ xác nhận";

    [Display(Name = "Tổng tiền")]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Chiết khấu")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "Tổng cộng")]
    public decimal FinalAmount { get; set; }

    [Display(Name = "Mã voucher")]
    public string? VoucherCode { get; set; }

    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";

    [Display(Name = "Ngày tạo")]
    public DateTime CreatedDate { get; set; }

    public DateTime OrderDate
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [Display(Name = "Ngày cập nhật")]
    public DateTime? UpdatedDate { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    public List<OrderItemAdminViewModel> OrderItems { get; set; } = new();

    public List<OrderItemAdminViewModel> Items
    {
        get => OrderItems;
        set => OrderItems = value;
    }

    public List<OrderItemAdminViewModel> OrderDetails
    {
        get => OrderItems;
        set => OrderItems = value ?? new();
    }

    public List<OrderCancellationViewModel> CancellationRequests { get; set; } = new();
}

public class UserOrderApiResponse
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
    public string? VoucherCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public List<UserOrderItemApiResponse> Items { get; set; } = new();
}

public class UserOrderItemApiResponse
{
    public Guid OrderDetailId { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? VariantName { get; set; }
    public decimal UnitPrice { get; set; }
    public byte Quantity { get; set; }
    public string? Note { get; set; }
}

public class OrderCancellationViewModel
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

public class OrderItemAdminViewModel
{
    public Guid OrderItemId { get; set; }
    public Guid OrderDetailId
    {
        get => OrderItemId;
        set => OrderItemId = value;
    }
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }

    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = string.Empty;

    public string? VariantName { get; set; }

    [Display(Name = "Đơn giá")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Số lượng")]
    public byte Quantity { get; set; }

    [Display(Name = "Thành tiền")]
    public decimal Total => UnitPrice * Quantity;
}
