using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? Image { get; set; }

        public int Quantity { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Sku { get; set; }

        public int? MinimumStock { get; set; }

        // Status: active, inactive
        [StringLength(30)]
        public string Status { get; set; } = "active";

        public virtual ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}

