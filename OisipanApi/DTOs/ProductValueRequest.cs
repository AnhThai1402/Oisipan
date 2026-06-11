using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductValueRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui long chon tuy chon san pham.")]
    public int ProductOptionId { get; set; }

    [Required(ErrorMessage = "Vui long nhap gia tri tuy chon.")]
    [StringLength(100, ErrorMessage = "Gia tri tuy chon khong duoc vuot qua 100 ky tu.")]
    public string ValueName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Gia tang them khong duoc am.")]
    public decimal AdditionalPrice { get; set; }
}
