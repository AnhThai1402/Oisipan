namespace Oishipan.DTOs;

public class ProductValueResponse
{
    public Guid ProductValueId { get; set; }
    public Guid ProductOptionId { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}
