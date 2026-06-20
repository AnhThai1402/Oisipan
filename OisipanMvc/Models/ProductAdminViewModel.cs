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

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [Display(Name = "Ảnh")]
    public string? Image { get; set; }

    [Display(Name = "Anh")]
    public IFormFile? ImageFile { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm.")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public List<ProductOptionAdminViewModel> ProductOptions { get; set; } = new();
    public List<ProductVariantAdminViewModel> ProductVariants { get; set; } = new();

    public List<SelectListItem> Categories { get; set; } = new();
}

public class ProductVariantAdminViewModel
{
    public int ProductVariantId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sản phẩm.")]
    [Display(Name = "Sản phẩm")]
    public int ProductId { get; set; }

    public string? ProductName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập size.")]
    [StringLength(100)]
    public string Size { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nhân bánh.")]
    [StringLength(100)]
    public string Filling { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Giá cộng thêm không được âm.")]
    public decimal AdditionalPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
    public int Quantity { get; set; }

    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Products { get; set; } = new();
}

public class ProductOptionAdminViewModel
{
    public int ProductOptionId { get; set; }
    public string OptionName { get; set; } = string.Empty;
    public List<ProductValueAdminViewModel> ProductValues { get; set; } = new();
}

public class ProductValueAdminViewModel
{
    public int ProductValueId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá trị biến thể.")]
    [StringLength(100, ErrorMessage = "Giá trị biến thể không được vượt quá 100 ký tự.")]
    public string ValueName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Giá cộng thêm không được âm.")]
    public decimal AdditionalPrice { get; set; }
}
