using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs
{
    public class OrderCancellationRequestDto
    {
        public Guid CancellationRequestId { get; set; }
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime RequestDate { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string? AdminNote { get; set; }
    }

    public class OrderCancellationRequestCreateDto
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do hủy đơn.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do hủy phải từ 10 đến 500 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminCancellationResponseDto
    {
        [Required]
        public bool IsApproved { get; set; }

        [StringLength(500)]
        public string? AdminNote { get; set; }
    }
}
