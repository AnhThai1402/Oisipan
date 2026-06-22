using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class CategoryRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
    public string CategoryName { get; set; } = string.Empty;

    public string? Image { get; set; }
}
