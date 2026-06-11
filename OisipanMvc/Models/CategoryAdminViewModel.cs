using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;

public class CategoryAdminViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string CategoryName { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public int ProductCount { get; set; }
}
