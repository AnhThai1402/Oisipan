using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class ProductVariant : BaseEntity
{
    [Key]
    public Guid ProductVariantId { get; set; }

    public Guid ProductId { get; set; }
    
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    public virtual ICollection<ProductVariantValue> ProductVariantValues { get; set; } = new List<ProductVariantValue>();

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public short StockQuantity { get; set; }

    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string? Sku { get; set; }

    [StringLength(900)]
    [Column(TypeName = "varchar(900)")]
    public string? CombinationKey { get; set; }

    // Designer feature: whether variant supports custom designer and design JSON data
    [Column(TypeName = "bit")]
    public bool IsDesignerEnabled { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? DesignData { get; set; }
}
