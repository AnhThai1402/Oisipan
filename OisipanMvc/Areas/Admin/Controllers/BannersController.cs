using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using FrontendMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BannersController : Controller
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IImageStorageService _imageStorageService;

    public BannersController(IHttpClientFactory httpClientFactory, IImageStorageService imageStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> Index()
    {
        var banners = await Api.GetFromJsonAsync<List<BannerViewModel>>(
            "api/banners?includeInactive=true") ?? new List<BannerViewModel>();
        return View(banners);
    }

    [HttpGet]
    public IActionResult Create() => View("CreateEdit", new BannerViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BannerViewModel model)
    {
        await SaveImageIfValid(model);
        if (!ModelState.IsValid) return View("CreateEdit", model);

        var response = await Api.PostAsJsonAsync("api/banners", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View("CreateEdit", model);
        }

        TempData["AdminMessage"] = "Thêm banner thành công.";
        TempData["AdminMessageType"] = "create";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var banner = await Api.GetFromJsonAsync<BannerViewModel>($"api/banners/{id}");
        return banner is null ? NotFound() : View("CreateEdit", banner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BannerViewModel model)
    {
        if (id != model.BannerId) return BadRequest();

        await SaveImageIfValid(model);
        if (!ModelState.IsValid) return View("CreateEdit", model);

        var response = await Api.PutAsJsonAsync($"api/banners/{id}", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View("CreateEdit", model);
        }

        TempData["AdminMessage"] = "Cập nhật banner thành công.";
        TempData["AdminMessageType"] = "edit";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await Api.DeleteAsync($"api/banners/{id}");
        if (response.IsSuccessStatusCode)
        {
            TempData["AdminMessage"] = "Xóa banner thành công.";
            TempData["AdminMessageType"] = "delete";
        }
        else
        {
            TempData["AdminMessage"] = "Không thể xóa banner.";
            TempData["AdminMessageType"] = "error";
        }
        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private static object ToRequest(BannerViewModel model) => new
    {
        model.Title,
        model.ImageUrl,
        model.Link,
        model.IsActive,
        model.DisplayOrder
    };

    private async Task SaveImageIfValid(BannerViewModel model)
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
            model.ImageUrl = await _imageStorageService.UploadBannerImageAsync(
                model.ImageFile, HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), $"Không thể tải ảnh: {ex.Message}");
        }
    }

    private async Task AddApiErrors(HttpResponseMessage response)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                foreach (var error in errors.EnumerateObject())
                foreach (var message in error.Value.EnumerateArray())
                    ModelState.AddModelError(error.Name, message.GetString() ?? "Dữ liệu không hợp lệ.");
                return;
            }
        }
        catch (JsonException)
        {
        }

        ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
    }
}
