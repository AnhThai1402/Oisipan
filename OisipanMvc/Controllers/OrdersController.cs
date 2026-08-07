using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var apiOrders = await Api.GetFromJsonAsyncWithOptions<List<UserOrderApiResponse>>($"api/orders/user/{userId}") ?? new();
        var orders = apiOrders.Select(order => MapToOrderViewModel(order, userId)).ToList();

        return View(orders);
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var apiOrder = await Api.GetFromJsonAsyncWithOptions<UserOrderApiResponse>($"api/orders/{id}");
        if (apiOrder is null)
        {
            return NotFound();
        }

        var order = MapToOrderViewModel(apiOrder, userId);

        // Verify that the order belongs to the current user
        if (order.UserId != userId)
        {
            return Forbid();
        }

        // Fetch cancellation requests for this order
        var cancellationRequests = await Api.GetFromJsonAsyncWithOptions<List<OrderCancellationViewModel>>($"api/orders/{id}/cancellation-requests") ?? new();
        order.CancellationRequests = cancellationRequests;

        return View(order);
    }

    public async Task<IActionResult> Print(Guid id, bool print = false)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var apiOrder = await Api.GetFromJsonAsyncWithOptions<UserOrderApiResponse>($"api/orders/{id}");
        if (apiOrder is null) return NotFound();

        var order = MapToOrderViewModel(apiOrder, userId);
        if (order.UserId != userId) return Forbid();

        ViewData["AutoPrint"] = print;
        return View("Invoice", order);
    }

    public async Task<IActionResult> DownloadInvoice(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var apiOrder = await Api.GetFromJsonAsyncWithOptions<UserOrderApiResponse>($"api/orders/{id}");
        if (apiOrder is null) return NotFound();

        var order = MapToOrderViewModel(apiOrder, userId);
        if (order.UserId != userId) return Forbid();

        var response = await Api.GetAsync($"api/orders/{id}/invoice.pdf");
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        return File(pdfBytes, "application/pdf", $"OP-{id:0000}-invoice.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCancellation(Guid id, [FromForm] string reason)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var trimmedReason = reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length < 10)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy ít nhất 10 ký tự.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // Verify that the order belongs to the current user
        var apiOrder = await Api.GetFromJsonAsyncWithOptions<UserOrderApiResponse>($"api/orders/{id}");
        if (apiOrder is null)
        {
            return NotFound();
        }

        var order = MapToOrderViewModel(apiOrder, userId);

        if (order.UserId != userId)
        {
            return Forbid();
        }

        // Send cancellation request to API
        var response = await Api.PostAsJsonAsync($"api/orders/{id}/cancellation-request", new { reason = trimmedReason });

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            TempData["ErrorMessage"] = ExtractMessage(errorContent) ?? "Không thể gửi yêu cầu hủy đơn lúc này.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        TempData["Message"] = "Yêu cầu hủy đơn hàng đã được gửi. Vui lòng chờ phản hồi từ admin.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private static string? ExtractMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Fall back to the raw response text.
        }

        return content.Length > 300 ? content[..300] : content;
    }

    private static OrderAdminViewModel MapToOrderViewModel(UserOrderApiResponse apiOrder, Guid userId)
    {
        return new OrderAdminViewModel
        {
            OrderId = apiOrder.OrderId,
            UserId = apiOrder.UserId,
            CustomerName = apiOrder.CustomerName ?? string.Empty,
            CustomerPhone = apiOrder.CustomerPhone ?? string.Empty,
            ShippingAddress = apiOrder.ShippingAddress ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(apiOrder.Status) ? "Chờ xác nhận" : apiOrder.Status,
            TotalAmount = apiOrder.TotalAmount,
            PaymentMethod = string.IsNullOrWhiteSpace(apiOrder.PaymentMethod) ? "COD" : apiOrder.PaymentMethod,
            CreatedDate = apiOrder.OrderDate,
            FinalAmount = apiOrder.TotalAmount,
            DiscountAmount = 0m,
            OrderItems = apiOrder.Items.Select(item => new OrderItemAdminViewModel
            {
                OrderDetailId = item.OrderDetailId,
                ProductVariantId = item.ProductVariantId,
                ProductId = item.ProductId,
                ProductName = item.ProductName ?? "—",
                VariantName = item.VariantName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                OrderItemId = item.OrderDetailId
            }).ToList()
        };
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
