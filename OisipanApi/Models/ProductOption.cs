using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class ProductOption : BaseEntity
    {
        [Key]
        public Guid ProductOptionId { get; set; }

        public Guid ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string OptionName { get; set; } = null!;

        public virtual ICollection<ProductValue> ProductValues { get; set; } = new List<ProductValue>();
    }
}
