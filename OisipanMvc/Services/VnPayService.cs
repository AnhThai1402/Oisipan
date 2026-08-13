using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using FrontendMvc.Options;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace FrontendMvc.Services;

public class VnPayService : IVnPayService
{
    private readonly VnPaySettings _settings;

    public VnPayService(IOptions<VnPaySettings> options)
    {
        _settings = options.Value;
    }

    public string CreatePaymentUrl(HttpContext httpContext, Guid orderId, decimal amount, string orderInfo)
    {
        if (string.IsNullOrWhiteSpace(_settings.TmnCode) || string.IsNullOrWhiteSpace(_settings.HashSecret))
        {
            throw new InvalidOperationException("Vui lòng cấu hình VnPay:TmnCode và VnPay:HashSecret trong appsettings.json.");
        }

        var returnUrl = string.IsNullOrWhiteSpace(_settings.ReturnUrl)
            ? UriHelper.BuildAbsolute(httpContext.Request.Scheme, httpContext.Request.Host, path: "/Cart/VnPayReturn")
            : _settings.ReturnUrl;

        var createDate = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var amountInVndSmallestUnit = ((long)(amount * 100m)).ToString(CultureInfo.InvariantCulture);

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = amountInVndSmallestUnit,
            ["vnp_CreateDate"] = createDate,
            ["vnp_CurrCode"] = _settings.CurrencyCode,
            ["vnp_IpAddr"] = GetClientIpAddress(httpContext),
            ["vnp_Locale"] = _settings.Locale,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = _settings.OrderType,
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = orderId.ToString("N"),
            ["vnp_ExpireDate"] = expireDate
        };

        if (!string.IsNullOrWhiteSpace(_settings.BankCode))
        {
            parameters["vnp_BankCode"] = _settings.BankCode.Trim();
        }

        var queryString = BuildQueryString(parameters);
        var secureHash = HmacSha512(_settings.HashSecret.Trim(), queryString);

        return $"{_settings.PaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";
    }

    public bool TryValidateReturn(IQueryCollection query, out VnPayReturnResult result)
    {
        result = new VnPayReturnResult();

        if (string.IsNullOrWhiteSpace(_settings.HashSecret))
        {
            return false;
        }

        var receivedHash = query["vnp_SecureHash"].ToString();
        if (string.IsNullOrWhiteSpace(receivedHash))
        {
            return false;
        }

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in query)
        {
            if (item.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                parameters[item.Key] = item.Value.ToString();
            }
        }

        var expectedHash = HmacSha512(_settings.HashSecret.Trim(), BuildQueryString(parameters));
        if (!string.Equals(receivedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var txnRef = query["vnp_TxnRef"].ToString();
        result.OrderId = Guid.TryParse(txnRef, out var orderId)
            ? orderId
            : Guid.TryParseExact(txnRef, "N", out orderId) ? orderId : null;
        result.ResponseCode = query["vnp_ResponseCode"].ToString();
        result.TransactionStatus = query["vnp_TransactionStatus"].ToString();
        result.TransactionNo = query["vnp_TransactionNo"].ToString();
        result.BankCode = query["vnp_BankCode"].ToString();

        if (decimal.TryParse(query["vnp_Amount"].ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var paidAmount))
        {
            result.Amount = paidAmount / 100m;
        }

        return true;
    }

    private static string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        return string.Join("&", parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
            .Select(parameter => $"{WebUtility.UrlEncode(parameter.Key)}={WebUtility.UrlEncode(parameter.Value)}"));
    }

    private static string HmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GetClientIpAddress(HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "::1" ? "127.0.0.1" : ipAddress;
    }
}
