using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductVariantUpdateRequest
{
    [StringLength(50)]
    public string? Sku { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, short.MaxValue)]
    public short StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ProductVariantBulkStatusRequest
{
    [Required]
    public List<Guid> VariantIds { get; set; } = new();

    public bool IsActive { get; set; }
}

public class ProductVariantBulkDeleteRequest
{
    [Required]
    public List<Guid> VariantIds { get; set; } = new();
}

public class ProductVariantBulkStockAdjustRequest
{
    [Required]
    public List<Guid> VariantIds { get; set; } = new();

    [Range(-100000, 100000)]
    public int DeltaQuantity { get; set; }
}
