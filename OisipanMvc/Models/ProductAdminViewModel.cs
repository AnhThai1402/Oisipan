using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Models;

public class ProductAdminViewModel
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    // Alias for views
    public string ProductName
    {
        get => Name;
        set => Name = value;
    }

    [StringLength(50)]
    public string? Sku { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [Display(Name = "Ảnh")]
    public string? Image { get; set; }

    // Alias for views
    public string? ImageUrl
    {
        get => Image;
        set => Image = value;
    }

    [Display(Name = "Ảnh tải lên")]
    public IFormFile? ImageFile { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
    [Display(Name = "Tồn kho")]
    public int Quantity { get; set; }

    // Alias for views
    public int Stock
    {
        get => Quantity;
        set => Quantity = value;
    }

    [Display(Name = "Tồn kho tối thiểu")]
    public int? MinimumStock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Display(Name = "Mô tả")]
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
            if (Quantity == 0) return "Hết hàng";
            if (MinimumStock.HasValue && Quantity <= MinimumStock) return "Sắp hết hàng";
            return "Đủ hàng";
        }
    }

    public List<SelectListItem> Categories { get; set; } = new();

    [Display(Name = "Biến thể sản phẩm")]
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
    public int ProductVariantId { get; set; }

    public int Id
    {
        get => ProductVariantId;
        set => ProductVariantId = value;
    }

    [Display(Name = "Tên tùy chọn")]
    public string OptionName { get; set; } = string.Empty;

    [Display(Name = "Giá trị")]
    public string Value { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public string? ProductName { get; set; }

    [Required]
    [StringLength(100)]
    public string Size { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Filling { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal AdditionalPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Products { get; set; } = new();
}

public class ProductOptionAdminViewModel
{
    public int ProductOptionId { get; set; }
    public int ProductId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<ProductValueAdminViewModel> ProductValues { get; set; } = new();
}

public class ProductValueAdminViewModel
{
    public int ProductValueId { get; set; }
    public int ProductOptionId { get; set; }

    [Required]
    [StringLength(100)]
    public string ValueName { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal AdditionalPrice { get; set; }
}

public class ProductOptionManagementViewModel
{
    public int? SelectedProductId { get; set; }
    public string? SelectedProductName { get; set; }
    public List<SelectListItem> Products { get; set; } = new();
    public List<ProductOptionAdminViewModel> Options { get; set; } = new();
}
