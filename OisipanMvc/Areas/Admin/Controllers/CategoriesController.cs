using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using FrontendMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : AdminBaseController
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

    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] string? search = null)
    {
        var categories = await Api.GetFromJsonAsyncWithOptions<List<CategoryAdminViewModel>>("api/categories") ?? new();
        if (!string.IsNullOrWhiteSpace(search))
        {
            categories = categories
                .Where(category => category.CategoryName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        const int pageSize = 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(categories.Count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var pagedCategories = categories.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = categories.Count;
        ViewBag.Search = search?.Trim();
        return View(pagedCategories);
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
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await GetApiErrorMessage(response, "Không thể tạo danh mục. Vui lòng kiểm tra lại dữ liệu."));
            return View("CreateEdit", model);
        }

        SetFlashMessage(
            "Tạo danh mục thành công",
            "create");

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<CategoryAdminViewModel>($"api/categories/{id}");
        return model is null ? NotFound() : View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryAdminViewModel model)
    {
        await SaveImageIfValid(model);
        if (!ModelState.IsValid) return View("CreateEdit", model);

        var response = await Api.PutAsJsonAsync($"api/categories/{id}", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await GetApiErrorMessage(response, "Không thể cập nhật danh mục. Vui lòng kiểm tra lại dữ liệu."));
            return View("CreateEdit", model);
        }

        SetFlashMessage(
            "Cập nhật danh mục thành công",
            "edit");

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<CategoryAdminViewModel>($"api/categories/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await Api.DeleteAsync($"api/categories/{id}");
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Xóa danh mục thành công" : "Không thể xóa danh mục đang có sản phẩm.",
            response.IsSuccessStatusCode ? "delete" : "warning");

        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private static object ToRequest(CategoryAdminViewModel model) => new
    {
        model.CategoryName,
        model.Image
    };

    private static async Task<string> GetApiErrorMessage(HttpResponseMessage response, string fallback)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return string.IsNullOrWhiteSpace(error?.Message) ? fallback : error.Message;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }

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
