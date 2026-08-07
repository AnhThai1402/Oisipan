using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Payment : BaseEntity
    {
        [Key]
        public Guid PaymentId { get; set; }

        public Guid OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? TransactionCode { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}