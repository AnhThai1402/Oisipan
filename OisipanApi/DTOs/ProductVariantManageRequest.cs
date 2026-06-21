using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductVariantManageRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

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
}
