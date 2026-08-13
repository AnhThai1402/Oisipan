using Microsoft.AspNetCore.Http;

namespace FrontendMvc.Services;

public interface IVnPayService
{
    string CreatePaymentUrl(HttpContext httpContext, Guid orderId, decimal amount, string orderInfo);
    bool TryValidateReturn(IQueryCollection query, out VnPayReturnResult result);
}

public class VnPayReturnResult
{
    public Guid? OrderId { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string TransactionNo { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsSuccess => ResponseCode == "00" && TransactionStatus == "00";
}
