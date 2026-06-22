using System.Net.Http.Json;
using FrontendMvc.Models;
using FrontendMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IImageStorageService _imageStorageService;

    public CategoriesController(
        IHttpClientFactory httpClientFactory,
        IImageStorageService imageStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await Api.GetFromJsonAsync<List<CategoryAdminViewModel>>("api/categories") ?? new();
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
        await SaveImageIfValid(model);
        if (!ModelState.IsValid) return View("CreateEdit", model);

        var response = await Api.PostAsJsonAsync("api/categories", ToRequest(model));
        TempData["Message"] = response.IsSuccessStatusCode
            ? "Tạo danh mục thành công"
            : "Không thể tạo danh mục";

        return response.IsSuccessStatusCode ? RedirectToAction(nameof(Index)) : View("CreateEdit", model);
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await Api.GetFromJsonAsync<CategoryAdminViewModel>($"api/categories/{id}");
        return model is null ? NotFound() : View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryAdminViewModel model)
    {
        await SaveImageIfValid(model);
        if (!ModelState.IsValid) return View("CreateEdit", model);

        var response = await Api.PutAsJsonAsync($"api/categories/{id}", ToRequest(model));
        TempData["Message"] = response.IsSuccessStatusCode
            ? "Cập nhật danh mục thành công"
            : "Không thể cập nhật danh mục";

        return response.IsSuccessStatusCode ? RedirectToAction(nameof(Index)) : View("CreateEdit", model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await Api.GetFromJsonAsync<CategoryAdminViewModel>($"api/categories/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/categories/{id}");
        TempData["Message"] = response.IsSuccessStatusCode
            ? "Xóa danh mục thành công"
            : "Không thể xóa danh mục đang có sản phẩm";

        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private static object ToRequest(CategoryAdminViewModel model) => new
    {
        model.CategoryName,
        model.Image
    };

    private async Task SaveImageIfValid(CategoryAdminViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0) return;

        if (!AllowedImageTypes.Contains(model.ImageFile.ContentType))
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Ảnh phải là JPG, PNG, WEBP hoặc GIF.");
            return;
        }

        if (model.ImageFile.Length > 4 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Dung lượng ảnh không được vượt quá 4MB.");
            return;
        }

        try
        {
            model.Image = await _imageStorageService.UploadCategoryImageAsync(
                model.ImageFile, HttpContext.RequestAborted);
            ModelState.Remove(nameof(model.Image));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            ModelState.AddModelError(nameof(model.ImageFile), $"Không thể tải ảnh: {ex.Message}");
        }
    }
}
