using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProductsController : Controller
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;

    public AdminProductsController(IHttpClientFactory httpClientFactory, IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var products = await Api.GetFromJsonAsync<List<ProductAdminViewModel>>("api/products") ?? new();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ProductAdminViewModel();
        await LoadOptions(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductAdminViewModel model)
    {
        await SaveImageIfValid(model);

        if (!ModelState.IsValid)
        {
            await LoadOptions(model);
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/products", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            await LoadOptions(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await Api.GetFromJsonAsync<ProductAdminViewModel>($"api/products/{id}");
        if (product is null)
        {
            return NotFound();
        }

        await LoadOptions(product);
        return View(product);
    }

    [HttpPost]
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
            await LoadOptions(model);
            return View(model);
        }

        var response = await Api.PutAsJsonAsync($"api/products/{id}", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            await LoadOptions(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/products/{id}");
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private static object ToRequest(ProductAdminViewModel model)
    {
        return new
        {
            model.Name,
            model.Price,
            model.Image,
            model.Quantity,
            model.CategoryId,
            model.Description
        };
    }

    private async Task LoadOptions(ProductAdminViewModel model)
    {
        var categories = await Api.GetFromJsonAsync<List<CategoryAdminViewModel>>("api/categories") ?? new();

        model.Categories = categories
            .Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString(), c.CategoryId == model.CategoryId))
            .ToList();
    }

    private async Task SaveImageIfValid(ProductAdminViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0)
        {
            return;
        }

        if (!AllowedImageTypes.Contains(model.ImageFile.ContentType))
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Anh san pham phai la JPG, PNG, WEBP hoac GIF.");
            return;
        }

        if (model.ImageFile.Length > 2 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Dung luong anh khong duoc vuot qua 2MB.");
            return;
        }

        var extension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadFolder);

        var filePath = Path.Combine(uploadFolder, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await model.ImageFile.CopyToAsync(stream);

        model.Image = $"/uploads/products/{fileName}";
    }

    private async Task AddApiErrors(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.TryGetProperty("errors", out var errors))
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

            if (root.TryGetProperty("message", out var messageProperty))
            {
                ModelState.AddModelError(string.Empty, messageProperty.GetString() ?? "Có lỗi xảy ra.");
                return;
            }
        }
        catch (JsonException)
        {
        }

        ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
    }
}
