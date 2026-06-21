namespace FrontendMvc.Models;

public class StorefrontViewModel
{
    public List<ProductCatalogViewModel> Products { get; set; } = new();
}

public class ProductCatalogViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public int Quantity { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public List<ProductOptionCatalogViewModel> ProductOptions { get; set; } = new();
    public List<ProductVariantCatalogViewModel> ProductVariants { get; set; } = new();
}

public class ProductOptionCatalogViewModel
{
    public int ProductOptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<ProductValueCatalogViewModel> ProductValues { get; set; } = new();
}

public class ProductValueCatalogViewModel
{
    public int ProductValueId { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}

public class ProductVariantCatalogViewModel
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Filling { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
}
