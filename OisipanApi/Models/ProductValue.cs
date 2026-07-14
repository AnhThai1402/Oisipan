using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class ProductValue : BaseEntity
    {
        [Key]
        public int ProductValueId { get; set; }

        public int ProductOptionId { get; set; }

        [ForeignKey("ProductOptionId")]
        public virtual ProductOption? ProductOption { get; set; }

        [Required]
        [StringLength(100)]
        public string ValueName { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; }
    }
}
