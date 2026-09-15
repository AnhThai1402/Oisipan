using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;


public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public short TotalQuantity => (short)Items.Sum(item => item.Quantity);
    public decimal TotalAmount => Items.Sum(item => item.LineTotal);
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lÃ²ng nháº­p há» tÃªn.")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lÃ²ng nháº­p sá»‘ Ä‘iá»‡n thoáº¡i.")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lÃ²ng nháº­p Ä‘á»‹a chá»‰ nháº­n bÃ¡nh.")]
    public string CustomerAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lÃ²ng chá»n phÆ°Æ¡ng thá»©c thanh toÃ¡n.")]
    public string PaymentMethod { get; set; } = "COD";

    public Guid? BuyNowProductVariantId { get; set; }
    
    public Guid? BuyNowProductId { get; set; }
    public int? BuyNowQuantity { get; set; }
    
    public string? VoucherCode { get; set; }
}

public class ApiOrderCreateRequest
{
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public string ShippingAddress { get; set; } = string.Empty;
    public string? VoucherCode { get; set; }
    public List<ApiOrderItemRequest> Items { get; set; } = new();
}

public class ApiOrderItemRequest
{
    public Guid ProductId { get; set; }
    public Guid ProductVariantId { get; set; }
    public byte Quantity { get; set; }
    public string? Note { get; set; }
}

public class ValidateVoucherViewModel
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public int TotalItems { get; set; }
}
