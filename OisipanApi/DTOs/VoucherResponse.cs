namespace Oishipan.DTOs
{
    public class VoucherResponse
    {
        public int VoucherId { get; set; }

        public string Code { get; set; } = null!;

        public decimal DiscountValue { get; set; }

        public int MinimumItems { get; set; }

        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
