namespace FrontendMvc.Models;

public class CartItemViewModel
{
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? Image { get; set; }
    public decimal UnitPrice { get; set; }
    public byte Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
