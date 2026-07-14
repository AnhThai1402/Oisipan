using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    [Table("CancellationReasons")]
    public class OrderCancellation : BaseEntity
    {
        [Key]
        public int OrderCancellationId { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(50)]
        public string? CancelledBy { get; set; }

        public DateTime CancelledAt { get; set; } = DateTime.Now;

        public DateTime? ResponseDate { get; set; }

        public string? AdminNote { get; set; }
    }
}
