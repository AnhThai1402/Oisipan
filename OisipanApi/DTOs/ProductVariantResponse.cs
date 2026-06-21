namespace Oishipan.DTOs;

public class ProductVariantResponse
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Filling { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
}
