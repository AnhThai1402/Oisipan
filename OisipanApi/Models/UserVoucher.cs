using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class UserVoucher : BaseEntity
    {
        [Key]
        public Guid UserVoucherId { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual Account? Account { get; set; }

        public Guid VoucherId { get; set; }
        [ForeignKey("VoucherId")]
        public virtual Voucher? Voucher { get; set; }

        // Whether this voucher has been used
        public bool IsUsed { get; set; } = false;

        // Reference to the order where this voucher was used (if any)
        public Guid? UsedInOrderId { get; set; }

        // Date when voucher was assigned to user
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        // Date when voucher was used
        public DateTime? UsedDate { get; set; }
    }
}
