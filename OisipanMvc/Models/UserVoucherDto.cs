using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models
{
    public class UserVoucherDto
    {
        public int UserVoucherId { get; set; }
        public int UserId { get; set; }
        public int VoucherId { get; set; }
        
        [Display(Name = "Mã voucher")]
        public string VoucherCode { get; set; } = string.Empty;
        
        [Display(Name = "Giảm giá")]
        public decimal DiscountValue { get; set; }

        public string DiscountType { get; set; } = "Fixed";
        
        [Display(Name = "Hết hạn")]
        public DateTime ExpiryDate { get; set; }
        
        [Display(Name = "Đã sử dụng")]
        public bool IsUsed { get; set; }
        
        [Display(Name = "Ngày sử dụng")]
        public DateTime? UsedDate { get; set; }
        
        [Display(Name = "Ngày nhận")]
        public DateTime AssignedDate { get; set; }
        
        [Display(Name = "Trạng thái")]
        public string Status
        {
            get
            {
                if (IsUsed)
                    return "Đã sử dụng";
                if (ExpiryDate < DateTime.Now)
                    return "Đã hết hạn";
                return "Có thể dùng";
            }
        }
    }
}
