using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;

public class VoucherAdminViewModel
{
    public int VoucherId { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal DiscountValue { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(20)]
    public string Status { get; set; } = "Active";
}
