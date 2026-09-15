using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductValueRequest
{
    public Guid ProductOptionId { get; set; }

    [Required(ErrorMessage = "Vui long nhap gia tri tuy chon.")]
    [StringLength(100, ErrorMessage = "Gia tri tuy chon khong duoc vuot qua 100 ky tu.")]
    public string ValueName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Gia tang them khong duoc am.")]
    public decimal AdditionalPrice { get; set; }
}
