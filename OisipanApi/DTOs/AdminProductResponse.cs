namespace Oishipan.DTOs;

/// <summary>
/// DTO specifically designed for Admin Product viewing - includes all necessary fields
/// </summary>
public class AdminProductResponse
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public int? MinimumStock { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public List<AdminProductOptionResponse> ProductOptions { get; set; } = new();
}

public class AdminProductOptionResponse
{
    public int ProductOptionId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<AdminProductValueResponse> ProductValues { get; set; } = new();
}

public class AdminProductValueResponse
{
    public int ProductValueId { get; set; }
    public int ProductOptionId { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}
