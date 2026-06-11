namespace Oishipan.DTOs;

public class ProductOptionResponse
{
    public int ProductOptionId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<ProductValueResponse> ProductValues { get; set; } = new();
}
