using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class ProductVariantValue : BaseEntity
{
    [Key]
    public Guid ProductVariantValueId { get; set; }

    public Guid ProductVariantId { get; set; }
    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariant? ProductVariant { get; set; }

    public Guid ProductValueId { get; set; }
    [ForeignKey(nameof(ProductValueId))]
    public ProductValue? ProductValue { get; set; }
}
