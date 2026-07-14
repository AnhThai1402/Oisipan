namespace Oishipan.DTOs;

public class ProductVariantResponse
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public List<ProductVariantValueResponse> VariantValues { get; set; } = new();
}

public class ProductVariantValueResponse
{
    public int ProductOptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public int ProductValueId { get; set; }
    public string ValueName { get; set; } = string.Empty;
}
