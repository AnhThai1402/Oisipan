using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Models;

public class ProductAdminViewModel
{
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "Vui lÃ²ng nháº­p tÃªn sáº£n pháº©m.")]
    [StringLength(200, ErrorMessage = "TÃªn sáº£n pháº©m khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 200 kÃ½ tá»±.")]
    [Display(Name = "TÃªn sáº£n pháº©m")]
    public string Name { get; set; } = string.Empty;

    // Alias for views
    public string ProductName
    {
        get => Name;
        set => Name = value;
    }

    [Range(0.01, double.MaxValue, ErrorMessage = "GiÃ¡ sáº£n pháº©m pháº£i lá»›n hÆ¡n 0.")]
    [Display(Name = "GiÃ¡")]
    public decimal Price { get; set; }

    [Display(Name = "áº¢nh")]
    public string? Image { get; set; }

    // Alias for views
    public string? ImageUrl
    {
        get => Image;
        set => Image = value;
    }

    [Display(Name = "áº¢nh táº£i lÃªn")]
    public IFormFile? ImageFile { get; set; }

    [Range(0, short.MaxValue, ErrorMessage = "Tá»“n kho khÃ´ng Ä‘Æ°á»£c Ã¢m.")]
    [Display(Name = "Tá»“n kho")]
    public short StockQuantity { get; set; }

    // Alias for views
    public int Stock
    {
        get => StockQuantity;
        set => StockQuantity = (short)value;
    }

    [Display(Name = "Tá»“n kho tá»‘i thiá»ƒu")]
    public byte? MinimumStock { get; set; }

    [Display(Name = "Danh má»¥c")]
    public Guid CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Display(Name = "MÃ´ táº£")]
    public string? Description { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "active"; // active, inactive

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }

    // Helper property for views
    public string StockStatus
    {
        get
        {
            if (StockQuantity == 0) return "Háº¿t hÃ ng";
            if (MinimumStock.HasValue && StockQuantity <= MinimumStock) return "Sáº¯p háº¿t hÃ ng";
            return "Äá»§ hÃ ng";
        }
    }

    public List<SelectListItem> Categories { get; set; } = new();

    [Display(Name = "Biáº¿n thá»ƒ sáº£n pháº©m")]
    public List<ProductVariantAdminViewModel> Variants { get; set; } = new();

    public List<ProductOptionAdminViewModel> ProductOptions { get; set; } = new();

    public List<ProductVariantAdminViewModel> ProductVariants
    {
        get => Variants;
        set => Variants = value ?? new();
    }
}

public class ProductVariantAdminViewModel
{
    public Guid ProductVariantId { get; set; }

    public Guid Id
    {
        get => ProductVariantId;
        set => ProductVariantId = value;
    }

    [Display(Name = "TÃªn tÃ¹y chá»n")]
    public string OptionName { get; set; } = string.Empty;

    [Display(Name = "GiÃ¡ trá»‹")]
    public string Value { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public string? ProductName { get; set; }

    public string? Sku { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, short.MaxValue)]
    public short StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Products { get; set; } = new();

    public List<ProductVariantValueAdminViewModel> VariantValues { get; set; } = new();
}

public class ProductVariantValueAdminViewModel
{
    public Guid ProductOptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public Guid ProductValueId { get; set; }
    public string ValueName { get; set; } = string.Empty;
}

public class ProductOptionAdminViewModel
{
    public Guid ProductOptionId { get; set; }
    public Guid ProductId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<ProductValueAdminViewModel> ProductValues { get; set; } = new();
}

public class ProductValueAdminViewModel
{
    public Guid ProductValueId { get; set; }
    public Guid ProductOptionId { get; set; }

    [Required]
    [StringLength(100)]
    public string ValueName { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal AdditionalPrice { get; set; }
}

public class ProductOptionManagementViewModel
{
    public Guid? SelectedProductId { get; set; }
    public string? SelectedProductName { get; set; }
    public List<SelectListItem> Products { get; set; } = new();
    public List<ProductOptionAdminViewModel> Options { get; set; } = new();
}
