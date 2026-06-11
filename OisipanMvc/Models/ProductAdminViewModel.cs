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

    public List<SelectListItem> Categories { get; set; } = new();
}
