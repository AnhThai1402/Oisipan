using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;

public class VoucherAdminViewModel
{
    public int VoucherId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã voucher.")]
    [StringLength(20, ErrorMessage = "Mã voucher không được vượt quá 20 ký tự.")]
    [Display(Name = "Mã voucher")]
    public string Code { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị chiết khấu phải lớn hơn 0.")]
    [Display(Name = "Giá trị chiết khấu")]
    public decimal DiscountValue { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối thiểu phải >= 0.")]
    [Display(Name = "Số lượng tối thiểu")]
    public int MinimumItems { get; set; } = 0;

    [Required(ErrorMessage = "Vui lòng chọn hạn sử dụng.")]
    [Display(Name = "Hạn sử dụng")]
    public DateTime ExpiryDate { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime CreatedDate { get; set; }
}
