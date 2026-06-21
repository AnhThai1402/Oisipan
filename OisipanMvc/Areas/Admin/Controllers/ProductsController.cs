using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using FrontendMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IImageStorageService _imageStorageService;

    public ProductsController(
        IHttpClientFactory httpClientFactory,
        IImageStorageService imageStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await Api.GetFromJsonAsync<List<ProductAdminViewModel>>("api/products") ?? new();
        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        var model = new ProductAdminViewModel();
        await PopulateCategories(model);
        return View("CreateEdit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductAdminViewModel model)
    {
        await SaveImageIfValid(model);
        if (!ModelState.IsValid)
        {
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        var response = await Api.PostAsJsonAsync("api/products", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            // Try to parse error details from API response
            try
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(errorContent))
                {
                    var errorJson = System.Text.Json.JsonDocument.Parse(errorContent);
                    var root = errorJson.RootElement;
                    
                    // Check for validation errors in ProblemDetails format
                    if (root.TryGetProperty("errors", out var errorsElement))
                    {
                        foreach (var property in errorsElement.EnumerateObject())
                        {
                            var fieldName = property.Name;
                            if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var error in property.Value.EnumerateArray())
                                {
                                    ModelState.AddModelError(fieldName, error.GetString() ?? "Lỗi không xác định");
                                }
                            }
                        }
                    }
                    else if (root.TryGetProperty("message", out var messageElement))
                    {
                        ModelState.AddModelError(string.Empty, messageElement.GetString() ?? "Không thể tạo sản phẩm");
                    }
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo sản phẩm. Vui lòng kiểm tra dữ liệu.");
            }

            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        TempData["Message"] = "Tạo sản phẩm thành công";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await Api.GetFromJsonAsync<ProductAdminViewModel>($"api/products/{id}");
        if (model is null) return NotFound();

        await PopulateCategories(model);
        return View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductAdminViewModel model)
    {
        if (id != model.ProductId)
        {
            return BadRequest();
        }

        await SaveImageIfValid(model);
        if (!ModelState.IsValid)
        {
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        var response = await Api.PutAsJsonAsync($"api/products/{id}", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Không thể cập nhật sản phẩm.");
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        TempData["Message"] = "Cập nhật sản phẩm thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await Api.GetFromJsonAsync<ProductAdminViewModel>($"api/products/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/products/{id}");
        TempData["Message"] = response.IsSuccessStatusCode
            ? "Xóa sản phẩm thành công"
            : "Không thể xóa sản phẩm";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategories(ProductAdminViewModel model)
    {
        var categories = await Api.GetFromJsonAsync<List<CategoryAdminViewModel>>("api/categories") ?? new();
        model.Categories = categories.Select(category => new SelectListItem
        {
            Value = category.CategoryId.ToString(),
            Text = category.CategoryName,
            Selected = category.CategoryId == model.CategoryId
        }).ToList();
    }

    private static object ToRequest(ProductAdminViewModel model) => new
    {
        model.Name,
        model.Price,
        model.Image,
        model.Quantity,
        model.CategoryId,
        model.Description
    };

    private async Task SaveImageIfValid(ProductAdminViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0)
        {
            return;
        }

        if (!AllowedImageTypes.Contains(model.ImageFile.ContentType))
        {
            ModelState.AddModelError(
                nameof(model.ImageFile),
                "Ảnh sản phẩm phải là JPG, PNG, WEBP hoặc GIF.");
            return;
        }

        if (model.ImageFile.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(
                nameof(model.ImageFile),
                "Dung lượng ảnh không được vượt quá 5MB.");
            return;
        }

        try
        {
            model.Image = await _imageStorageService.UploadProductImageAsync(
                model.ImageFile,
                HttpContext.RequestAborted);
            ModelState.Remove(nameof(model.Image));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            ModelState.AddModelError(nameof(model.ImageFile), $"Không thể tải ảnh: {ex.Message}");
        }
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
