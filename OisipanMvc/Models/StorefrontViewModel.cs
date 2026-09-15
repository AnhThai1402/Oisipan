namespace FrontendMvc.Models;

public class StorefrontViewModel
{
    public List<ProductCatalogViewModel> Products { get; set; } = new();

    public List<CategoryAdminViewModel> Categories { get; set; } = new();
    public Guid? SelectedCategoryId { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
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
}
