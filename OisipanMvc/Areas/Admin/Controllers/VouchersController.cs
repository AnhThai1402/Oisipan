using System.Net.Http.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VouchersController : AdminBaseController
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

        SetFlashMessage("Tạo voucher thành công", "create");
        return RedirectToAction("Index");
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<VoucherAdminViewModel>($"api/vouchers/{id}");
        if (model is null) return NotFound();

        return View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, VoucherAdminViewModel model)
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

        SetFlashMessage("Cập nhật voucher thành công", "edit");
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<VoucherAdminViewModel>($"api/vouchers/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await Api.DeleteAsync($"api/vouchers/{id}");
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Xóa voucher thành công" : "Không thể xóa voucher.",
            response.IsSuccessStatusCode ? "delete" : "error");

        return RedirectToAction("Index");
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
