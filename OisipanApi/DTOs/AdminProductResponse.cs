namespace Oishipan.DTOs;

/// <summary>
/// DTO specifically designed for Admin Product viewing - includes all necessary fields
/// </summary>
public class AdminProductResponse
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public string? Sku { get; set; }
    public short StockQuantity { get; set; }
    public byte? MinimumStock { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public List<AdminProductOptionResponse> ProductOptions { get; set; } = new();
}

public class AdminProductOptionResponse
{
    public Guid ProductOptionId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<AdminProductValueResponse> ProductValues { get; set; } = new();
}

public class AdminProductValueResponse
{
    public Guid ProductValueId { get; set; }
    public Guid ProductOptionId { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}
