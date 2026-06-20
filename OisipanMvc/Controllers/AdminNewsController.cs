using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using FrontendMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminNewsController : Controller
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IImageStorageService _imageStorageService;

    public AdminNewsController(
        IHttpClientFactory httpClientFactory,
        IImageStorageService imageStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> Index()
    {
        var articles = await Api.GetFromJsonAsync<List<NewsArticleViewModel>>(
            "api/newsarticles?includeUnpublished=true") ?? new();
        return View(articles);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new NewsArticleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NewsArticleViewModel model)
    {
        await SaveImageIfValid(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/newsarticles", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        TempData["SuccessMessage"] = "Thêm bài viết thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await Api.GetFromJsonAsync<NewsArticleViewModel>(
            $"api/newsarticles/{id}?includeUnpublished=true");
        return article is null ? NotFound() : View(article);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NewsArticleViewModel model)
    {
        if (id != model.NewsArticleId)
        {
            return BadRequest();
        }

        await SaveImageIfValid(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PutAsJsonAsync($"api/newsarticles/{id}", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật bài viết thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/newsarticles/{id}");
        TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccessStatusCode ? "Xóa bài viết thành công." : "Không thể xóa bài viết.";
        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private static object ToRequest(NewsArticleViewModel model)
    {
        return new
        {
            model.Title,
            model.Summary,
            model.Content,
            model.Image,
            model.IsPublished,
            model.PublishedAt
        };
    }

    private async Task SaveImageIfValid(NewsArticleViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0)
        {
            return;
        }

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
            model.Image = await _imageStorageService.UploadNewsImageAsync(
                model.ImageFile,
                HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), $"Không thể tải ảnh: {ex.Message}");
        }
    }

    private async Task AddApiErrors(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                foreach (var error in errors.EnumerateObject())
                {
                    foreach (var message in error.Value.EnumerateArray())
                    {
                        ModelState.AddModelError(error.Name, message.GetString() ?? "Dữ liệu không hợp lệ.");
                    }
                }
                return;
            }
        }
        catch (JsonException)
        {
        }

        ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
    }
}
