using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Order : BaseEntity
    {
        [Key]
        public Guid OrderId { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Account? Account { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string CustomerPhone { get; set; } = string.Empty;

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string PaymentMethod { get; set; } = null!;

        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string ShippingAddress { get; set; } = string.Empty;

        public Guid? VoucherId { get; set; }

        [ForeignKey("VoucherId")]
        public virtual Voucher? Voucher { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string OrderStatus { get; set; } = "Chờ xác nhận";
    }
}
