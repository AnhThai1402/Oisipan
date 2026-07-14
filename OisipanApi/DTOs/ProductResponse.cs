namespace Oishipan.DTOs;

public class ProductResponse
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public int? MinimumStock { get; set; }
    public string Status { get; set; } = "active";
    public List<ProductOptionResponse> ProductOptions { get; set; } = new();
    public List<ProductVariantResponse> ProductVariants { get; set; } = new();
}
