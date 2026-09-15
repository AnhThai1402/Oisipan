using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class OrderDetail : BaseEntity
    {
        [Key]
        public Guid OrderDetailId { get; set; }

        public Guid OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public Guid ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public byte Quantity { get; set; }

        [StringLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string? ProductName { get; set; }

        [StringLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string? VariantName { get; set; }

        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? Note { get; set; }
    }
}