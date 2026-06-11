using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        [StringLength(50)]
        public string Vnp_TransactionNo { get; set; } = null!;

        [Required]
        [StringLength(10)]
        public string Vnp_ResponseCode { get; set; } = null!;

        public DateTime PaymentDate { get; set; }
    }
}