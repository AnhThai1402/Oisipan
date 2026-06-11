using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductOptionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui long chon san pham.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui long nhap ten tuy chon.")]
    [StringLength(100, ErrorMessage = "Ten tuy chon khong duoc vuot qua 100 ky tu.")]
    public string OptionName { get; set; } = string.Empty;
}
