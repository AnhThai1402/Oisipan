namespace FrontendMvc.Models;

public class VariantFormViewModel
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string Mode { get; set; } = "add";
    public Guid? VariantId { get; set; }
    public decimal Price { get; set; }
    public short StockQuantity { get; set; }
    public List<VariantFormAttribute> Attributes { get; set; } = new();
}

public class VariantFormAttribute
{
    public string Name { get; set; } = string.Empty;
    public string Values { get; set; } = string.Empty;
}
