using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class ProductVariantValue : BaseEntity
{
    [Key]
    public int ProductVariantValueId { get; set; }

    public int ProductVariantId { get; set; }
    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariant? ProductVariant { get; set; }

    public int ProductValueId { get; set; }
    [ForeignKey(nameof(ProductValueId))]
    public ProductValue? ProductValue { get; set; }
}
