namespace Oishipan.DTOs;

public class ProductValueResponse
{
    public int ProductValueId { get; set; }
    public int ProductOptionId { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}
