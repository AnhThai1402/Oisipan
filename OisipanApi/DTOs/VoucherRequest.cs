using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs
{
    public class VoucherRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mã voucher.")]
        [StringLength(20, ErrorMessage = "Mã voucher không được vượt quá 20 ký tự.")]
        public string Code { get; set; } = null!;

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị chiết khấu phải lớn hơn 0.")]
        public decimal DiscountValue { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tối thiểu phải >= 0.")]
        public int MinimumItems { get; set; } = 0; // Minimum number of items required

        [Required(ErrorMessage = "Vui lòng chọn hạn sử dụng.")]
        public DateTime ExpiryDate { get; set; }
    }
}
