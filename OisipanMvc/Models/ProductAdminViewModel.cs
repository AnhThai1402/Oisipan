using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Models;

public class ProductAdminViewModel
{
    public Guid ProductId { get; set; }

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

    [Range(0, short.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
    [Display(Name = "Tồn kho")]
    public short StockQuantity { get; set; }

    // Alias for views
    public int Stock
    {
        get => StockQuantity;
        set => StockQuantity = (short)value;
    }

    [Display(Name = "Tồn kho tối thiểu")]
    public byte? MinimumStock { get; set; }

    [Display(Name = "Danh mục")]
    public Guid CategoryId { get; set; }

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
            if (StockQuantity == 0) return "Hết hàng";
            if (MinimumStock.HasValue && StockQuantity <= MinimumStock) return "Sắp hết hàng";
            return "Đủ hàng";
        }
    }

    public List<SelectListItem> Categories { get; set; } = new();

    public List<string>? RemovedImageUrls { get; set; } = new();
}
