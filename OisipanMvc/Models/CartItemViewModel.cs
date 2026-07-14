namespace FrontendMvc.Models;

public class CartItemViewModel
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? Image { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
