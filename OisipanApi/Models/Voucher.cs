using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Voucher : BaseEntity
    {
        [Key]
        public Guid VoucherId { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Code { get; set; } = null!;

        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string DiscountType { get; set; } = "Fixed"; // "Percentage" or "Fixed"

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string VoucherType { get; set; } = "Public"; // e.g., "Công khai"

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string DistributionMethod { get; set; } = "SaveOnPage"; // e.g., "Tự lưu trên trang"

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxDiscount { get; set; } = 0; // 0 = no limit

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderValue { get; set; } = 0;

        public byte MinimumItems { get; set; } = 0; // Kept for backwards compatibility if needed, or we can use MinOrderValue

        public short TotalQuantity { get; set; } = 100;

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime ExpiryDate { get; set; }

        public bool IsExpired
        {
            get => ExpiryDate < DateTime.Now;
        }
    }
}