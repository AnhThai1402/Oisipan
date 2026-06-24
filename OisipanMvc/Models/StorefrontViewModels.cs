using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;

public class StorefrontViewModel
{
    public List<ProductCatalogViewModel> Products { get; set; } = new();
}

public class ProductCatalogViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public int Quantity { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
}

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? Image { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

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
}

public class ApiOrderCreateRequest
{
    public int UserId { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public string ShippingAddress { get; set; } = string.Empty;
    public List<ApiOrderItemRequest> Items { get; set; } = new();
}

public class ApiOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
