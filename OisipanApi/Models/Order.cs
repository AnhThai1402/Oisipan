using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Account? Account { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Chờ xác nhận";

        [StringLength(50)]
        public string PaymentMethod { get; set; } = null!;

        [StringLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

