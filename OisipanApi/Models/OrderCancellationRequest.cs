using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class OrderCancellationRequest
    {
        [Key]
        public int CancellationRequestId { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        // Status: Pending, Approved, Rejected
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public DateTime? ResponseDate { get; set; }

        public string? AdminNote { get; set; }
    }
}
