using System.Net.Http.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : AdminBaseController
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CategoriesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await Api.GetFromJsonAsyncWithOptions<List<CategoryAdminViewModel>>("api/categories") ?? new();
        return View(categories);
    }

    public IActionResult Create()
    {
        return View("CreateEdit", new CategoryAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryAdminViewModel model)
    {
        if (!ModelState.IsValid) return View("CreateEdit", model);

        var response = await Api.PostAsJsonAsync("api/categories", model);
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Tạo danh mục thành công" : "Không thể tạo danh mục. Vui lòng kiểm tra lại dữ liệu.",
            response.IsSuccessStatusCode ? "create" : "error");

        return response.IsSuccessStatusCode ? RedirectToAction(nameof(Index)) : View("CreateEdit", model);
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<CategoryAdminViewModel>($"api/categories/{id}");
        return model is null ? NotFound() : View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryAdminViewModel model)
    {
        if (!ModelState.IsValid) return View("CreateEdit", model);

        var response = await Api.PutAsJsonAsync($"api/categories/{id}", model);
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Cập nhật danh mục thành công" : "Không thể cập nhật danh mục. Vui lòng kiểm tra lại dữ liệu.",
            response.IsSuccessStatusCode ? "edit" : "error");

        return response.IsSuccessStatusCode ? RedirectToAction(nameof(Index)) : View("CreateEdit", model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<CategoryAdminViewModel>($"api/categories/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/categories/{id}");
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Xóa danh mục thành công" : "Không thể xóa danh mục đang có sản phẩm.",
            response.IsSuccessStatusCode ? "delete" : "warning");

        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
