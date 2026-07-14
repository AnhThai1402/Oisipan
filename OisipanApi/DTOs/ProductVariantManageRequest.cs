using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductVariantManageRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    public List<int> ProductValueIds { get; set; } = new();

    [StringLength(50)]
    public string? Sku { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}
