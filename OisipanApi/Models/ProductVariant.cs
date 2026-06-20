using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class ProductVariant
{
    [Key]
    public int ProductVariantId { get; set; }

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Required]
    [StringLength(100)]
    public string Size { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Filling { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdditionalPrice { get; set; }

    public int Quantity { get; set; }

    public bool IsActive { get; set; } = true;
}
