using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Product : BaseEntity
    {
        [Key]
        public Guid ProductId { get; set; }

        [Required]
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string Name { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "varchar(max)")]
        public string? Image { get; set; }

        public short StockQuantity { get; set; }

        public Guid CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? Sku { get; set; }

        public byte? MinimumStock { get; set; }

        public virtual ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    }
}
