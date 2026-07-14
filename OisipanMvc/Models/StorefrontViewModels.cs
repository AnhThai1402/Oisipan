using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;


public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public int TotalQuantity => Items.Sum(item => item.Quantity);
    public decimal TotalAmount => Items.Sum(item => item.LineTotal);
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận bánh.")]
    public string CustomerAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    public string PaymentMethod { get; set; } = "COD";

    public int? BuyNowProductVariantId { get; set; }
    
    public string? VoucherCode { get; set; }
}

public class ApiOrderCreateRequest
{
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public string ShippingAddress { get; set; } = string.Empty;
    public string? VoucherCode { get; set; }
    public List<ApiOrderItemRequest> Items { get; set; } = new();
}

public class ApiOrderItemRequest
{
    public int ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}

public class ValidateVoucherViewModel
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public int TotalItems { get; set; }
}
