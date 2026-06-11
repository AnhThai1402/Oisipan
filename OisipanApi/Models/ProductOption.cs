using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class ProductOption
    {
        [Key]
        public int ProductOptionId { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string OptionName { get; set; } = null!;

        public virtual ICollection<ProductValue> ProductValues { get; set; } = new List<ProductValue>();
    }
}
