using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductVariantManageRequest
{
    public Guid ProductId { get; set; }

    [Required]
    public List<Guid> ProductValueIds { get; set; } = new();

    [StringLength(50)]
    public string? Sku { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, short.MaxValue)]
    public short StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}
