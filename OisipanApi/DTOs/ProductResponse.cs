namespace Oishipan.DTOs;

public class ProductResponse
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public int Quantity { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public List<ProductOptionResponse> ProductOptions { get; set; } = new();
    public List<ProductVariantResponse> ProductVariants { get; set; } = new();
}

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
