using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class UserVoucher
{
    [Key]
    public int UserVoucherId { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public Account? Account { get; set; }

    public int VoucherId { get; set; }

    [ForeignKey(nameof(VoucherId))]
    public Voucher? Voucher { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.Now;
    public bool IsUsed { get; set; }
    public DateTime? UsedDate { get; set; }
    public int? UsedInOrderId { get; set; }
}
