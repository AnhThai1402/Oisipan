using System.Net.Http.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VouchersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public VouchersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var vouchers = await Api.GetFromJsonAsyncWithOptions<List<VoucherAdminViewModel>>("api/vouchers") ?? new();
        return View(vouchers);
    }

    public IActionResult Create()
    {
        return View("CreateEdit", new VoucherAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VoucherAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CreateEdit", model);
        }

        var response = await Api.PostAsJsonAsync("api/vouchers", model);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Không thể tạo voucher. Vui lòng kiểm tra dữ liệu.");
            return View("CreateEdit", model);
        }

        TempData["Message"] = "Tạo voucher thành công";
        return RedirectToAction("Index");
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<VoucherAdminViewModel>($"api/vouchers/{id}");
        if (model is null) return NotFound();

        return View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VoucherAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CreateEdit", model);
        }

        var response = await Api.PutAsJsonAsync($"api/vouchers/{id}", model);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Không thể cập nhật voucher.");
            return View("CreateEdit", model);
        }

        TempData["Message"] = "Cập nhật voucher thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<VoucherAdminViewModel>($"api/vouchers/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/vouchers/{id}");
        TempData["Message"] = response.IsSuccessStatusCode
            ? "Xóa voucher thành công"
            : "Không thể xóa voucher";

        return RedirectToAction("Index");
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
