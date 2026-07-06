using System.Net.Http.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : AdminBaseController
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await Api.GetFromJsonAsyncWithOptions<List<OrderAdminViewModel>>("api/orders/admin/orders") ?? new();
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await Api.GetFromJsonAsyncWithOptions<OrderAdminViewModel>($"api/orders/admin/{id}");
        if (order is null)
        {
            return NotFound();
        }

        // Fetch cancellation requests for this order
        var cancellationRequests = await Api.GetFromJsonAsyncWithOptions<List<OrderCancellationRequestViewModel>>($"api/orders/{id}/cancellation-requests") ?? new();
        order.CancellationRequests = cancellationRequests;

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var response = await Api.PatchAsJsonAsyncWithOptions($"api/orders/{id}/status", new { status });
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Cập nhật trạng thái đơn hàng thành công." : "Không thể cập nhật trạng thái đơn hàng. Vui lòng thử lại.",
            response.IsSuccessStatusCode ? "edit" : "error");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondCancellation(int cancellationRequestId, string orderId, bool approve, string? adminNote)
    {
        var response = await Api.PatchAsJsonAsyncWithOptions($"api/orders/cancellation-request/{cancellationRequestId}/respond", 
            new { isApproved = approve, adminNote = adminNote });
        
        SetFlashMessage(
            response.IsSuccessStatusCode
                ? (approve ? "Yêu cầu hủy đơn đã được chấp nhận." : "Yêu cầu hủy đơn đã bị từ chối.")
                : "Không thể xử lý yêu cầu. Vui lòng thử lại.",
            response.IsSuccessStatusCode ? "edit" : "error");

        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
