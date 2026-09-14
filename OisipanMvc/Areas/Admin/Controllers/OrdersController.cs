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

    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await Api.GetFromJsonAsyncWithOptions<OrderAdminViewModel>($"api/orders/admin/{id}");
        if (order is null)
        {
            return NotFound();
        }

        // Fetch cancellation requests for this order
        var cancellationRequests = await Api.GetFromJsonAsyncWithOptions<List<OrderCancellationViewModel>>($"api/orders/{id}/cancellation-requests") ?? new();
        order.CancellationRequests = cancellationRequests;

        return View(order);
    }

    public async Task<IActionResult> Print(Guid id, bool print = false, bool download = false)
    {
        var order = await Api.GetFromJsonAsyncWithOptions<OrderAdminViewModel>($"api/orders/admin/{id}");
        if (order is null)
        {
            return NotFound();
        }

        if (print)
        {
            ViewData["AutoPrint"] = true;
        }

        if (download)
        {
            ViewData["AutoDownload"] = true;
        }

        return View("~/Views/Orders/Invoice.cshtml", order);
    }

    public async Task<IActionResult> DownloadInvoice(Guid id)
    {
        var order = await Api.GetFromJsonAsyncWithOptions<OrderAdminViewModel>($"api/orders/admin/{id}");
        if (order is null)
        {
            return NotFound();
        }

        var response = await Api.GetAsync($"api/orders/{id}/invoice.pdf");
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        return File(pdfBytes, "application/pdf", $"OP-{id.ToString("N")}-invoice.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        var response = await Api.PatchAsJsonAsyncWithOptions($"api/orders/{id}/status", new { status });
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Cập nhật trạng thái đơn hàng thành công." : "Không thể cập nhật trạng thái đơn hàng. Vui lòng thử lại.",
            response.IsSuccessStatusCode ? "edit" : "error");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondCancellation(Guid cancellationRequestId, string orderId, bool approve, string? adminNote)
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
