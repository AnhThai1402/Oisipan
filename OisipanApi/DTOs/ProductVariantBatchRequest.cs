namespace Oishipan.DTOs;

public class ProductVariantAttributeInput
{
    public string Name { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}

public class ProductVariantAddBatchRequest
{
    public List<ProductVariantAttributeInput> Attributes { get; set; } = new();
    public decimal Price { get; set; }
    public short StockQuantity { get; set; }
}

public class ProductVariantFullUpdateRequest
{
    public List<ProductVariantAttributeInput> Attributes { get; set; } = new();
    public decimal Price { get; set; }
    public short StockQuantity { get; set; }
}
