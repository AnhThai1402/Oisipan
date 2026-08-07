namespace Oishipan.DTOs;

public class ProductOptionResponse
{
    public Guid ProductOptionId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<ProductValueResponse> ProductValues { get; set; } = new();
}
