using System.Net.Http.Json;
using System.Security.Claims;
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
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var orders = await Api.GetFromJsonAsync<List<OrderAdminViewModel>>($"api/orders/user/{userId}") ?? new();
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var order = await Api.GetFromJsonAsync<OrderAdminViewModel>($"api/orders/{id}");
        if (order is null)
        {
            return NotFound();
        }

        // Verify that the order belongs to the current user
        if (order.UserId != userId)
        {
            return Forbid();
        }

        // Fetch cancellation requests for this order
        var cancellationRequests = await Api.GetFromJsonAsync<List<OrderCancellationRequestViewModel>>($"api/orders/{id}/cancellation-requests") ?? new();
        order.CancellationRequests = cancellationRequests;

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCancellation(int id, string reason)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        // Verify that the order belongs to the current user
        var order = await Api.GetFromJsonAsync<OrderAdminViewModel>($"api/orders/{id}");
        if (order is null)
        {
            return NotFound();
        }

        if (order.UserId != userId)
        {
            return Forbid();
        }

        // Send cancellation request to API
        var response = await Api.PostAsJsonAsync($"api/orders/{id}/cancellation-request", new { reason = reason });

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return BadRequest(new { message = errorContent });
        }

        TempData["Message"] = "Yêu cầu hủy đơn hàng đã được gửi. Vui lòng chờ phản hồi từ admin.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
