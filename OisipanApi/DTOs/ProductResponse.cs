namespace Oishipan.DTOs;

public class ProductResponse
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public string? Sku { get; set; }
    public short StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public byte? MinimumStock { get; set; }
    public string Status { get; set; } = "active";
    public List<ProductOptionResponse> ProductOptions { get; set; } = new();
    public List<ProductVariantResponse> ProductVariants { get; set; } = new();
}
