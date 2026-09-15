namespace FrontendMvc.Models;

public class StorefrontViewModel
{
    public List<ProductCatalogViewModel> Products { get; set; } = new();
    public List<BannerViewModel> Banners { get; set; } = new();
    public List<CategoryAdminViewModel> Categories { get; set; } = new();
    public Guid? SelectedCategoryId { get; set; }
}

public class ProductCatalogViewModel
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public string? Sku { get; set; }
    public short StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public List<ProductOptionCatalogViewModel> ProductOptions { get; set; } = new();
    public List<ProductVariantCatalogViewModel> ProductVariants { get; set; } = new();
}

public class ProductOptionCatalogViewModel
{
    public Guid ProductOptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<ProductValueCatalogViewModel> ProductValues { get; set; } = new();
}

public class ProductValueCatalogViewModel
{
    public Guid ProductValueId { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
}

public class ProductVariantCatalogViewModel
{
    public Guid ProductVariantId { get; set; }
    public Guid ProductId { get; set; }
    public string? Sku { get; set; }
    public List<ProductVariantValueCatalogViewModel> VariantValues { get; set; } = new();
    public decimal Price { get; set; }
    public short StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class ProductVariantValueCatalogViewModel
{
    public Guid ProductOptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public Guid ProductValueId { get; set; }
    public string ValueName { get; set; } = string.Empty;
}
