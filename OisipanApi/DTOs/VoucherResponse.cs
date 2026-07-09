namespace Oishipan.DTOs
{
    public class VoucherResponse
    {
        public int VoucherId { get; set; }

        public string Code { get; set; } = null!;

        public decimal DiscountValue { get; set; }

        public int MinimumItems { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string Name { get; set; } = string.Empty;

        public string DiscountType { get; set; } = string.Empty;

        public string VoucherType { get; set; } = string.Empty;

        public string DistributionMethod { get; set; } = string.Empty;

        public decimal MaxDiscount { get; set; }

        public decimal MinOrderValue { get; set; }

        public int TotalQuantity { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
