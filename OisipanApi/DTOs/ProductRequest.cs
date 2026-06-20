using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
    public decimal Price { get; set; }

    public string? Image { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm.")]
    public int Quantity { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public int CategoryId { get; set; }

    public string? Description { get; set; }
}
