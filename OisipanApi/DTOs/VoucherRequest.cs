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

        public string Name { get; set; } = string.Empty;

        public string DiscountType { get; set; } = string.Empty;

        public string VoucherType { get; set; } = string.Empty;

        public string DistributionMethod { get; set; } = string.Empty;

        public decimal MaxDiscount { get; set; }

        public decimal MinOrderValue { get; set; }

        public int TotalQuantity { get; set; }

        public DateTime StartDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class ValidateVoucherRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
        public int TotalItems { get; set; }
    }
}
