namespace FrontendMvc.Options;

public class VnPaySettings
{
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "2.1.0";
    public string Locale { get; set; } = "vn";
    public string CurrencyCode { get; set; } = "VND";
    public string OrderType { get; set; } = "other";
    public string? BankCode { get; set; }
}
