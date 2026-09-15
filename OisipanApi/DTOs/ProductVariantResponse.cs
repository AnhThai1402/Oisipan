namespace Oishipan.DTOs;

public class ProductVariantResponse
{
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public short StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public List<ProductVariantValueResponse> VariantValues { get; set; } = new();
}

public class ProductVariantValueResponse
{
    public Guid ProductOptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public Guid ProductValueId { get; set; }
    public string ValueName { get; set; } = string.Empty;
}
