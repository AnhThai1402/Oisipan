using System.Net.Http.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await Api.GetFromJsonAsync<List<OrderAdminViewModel>>("api/orders") ?? new();
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await Api.GetFromJsonAsync<OrderAdminViewModel>($"api/orders/{id}");
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var response = await Api.PatchAsJsonAsync($"api/orders/{id}/status", new { status });
        TempData["Message"] = response.IsSuccessStatusCode
            ? "Cập nhật trạng thái đơn hàng thành công."
            : "Không thể cập nhật trạng thái đơn hàng. Vui lòng thử lại.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
