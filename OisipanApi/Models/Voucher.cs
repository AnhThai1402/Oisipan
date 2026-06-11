using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Status: Active, Inactive, Expired
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsExpired
        {
            get => ExpiryDate < DateTime.Now;
        }
    }
}