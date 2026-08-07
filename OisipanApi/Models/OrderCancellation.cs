using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    [Table("CancellationReasons")]
    public class OrderCancellation : BaseEntity
    {
        [Key]
        public Guid OrderCancellationId { get; set; }

        public Guid OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string? CancelledBy { get; set; }

        public DateTime CancelledAt { get; set; } = DateTime.Now;

        public DateTime? ResponseDate { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? AdminNote { get; set; }

        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string RequestStatus { get; set; } = "Pending";
    }
}
