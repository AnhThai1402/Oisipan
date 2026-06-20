using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Services;
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
    private readonly IImageStorageService _imageStorageService;

    public AdminProductsController(IHttpClientFactory httpClientFactory, IImageStorageService imageStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await Api.GetFromJsonAsync<List<ProductAdminViewModel>>("api/products") ?? new();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ProductAdminViewModel
        {
            ProductVariants = [new() { IsActive = true }]
        };
        await LoadOptions(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductAdminViewModel model)
    {
        NormalizeAndValidateProductVariants(model);
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

        if (product.ProductVariants.Count == 0)
        {
            product.ProductVariants.Add(new ProductVariantAdminViewModel { IsActive = true });
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

        NormalizeAndValidateProductVariants(model);
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
            model.Description,
            ProductVariants = model.ProductVariants.Select(variant => new
            {
                variant.ProductVariantId,
                variant.Size,
                variant.Filling,
                variant.AdditionalPrice,
                variant.Quantity,
                variant.IsActive
            })
        };
    }

    private void NormalizeAndValidateProductVariants(ProductAdminViewModel model)
    {
        model.ProductVariants ??= new();
        model.ProductVariants = model.ProductVariants
            .Where(variant =>
                !string.IsNullOrWhiteSpace(variant.Size) ||
                !string.IsNullOrWhiteSpace(variant.Filling))
            .ToList();

        if (model.ProductVariants.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.ProductVariants),
                "Vui lòng thêm ít nhất một biến thể sản phẩm.");
            return;
        }

        var hasDuplicates = model.ProductVariants
            .GroupBy(
                variant => $"{variant.Size.Trim()}|{variant.Filling.Trim()}",
                StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (hasDuplicates)
        {
            ModelState.AddModelError(
                nameof(model.ProductVariants),
                "Tổ hợp size và nhân bánh không được trùng nhau.");
        }

        model.Quantity = model.ProductVariants.Sum(variant => variant.Quantity);
    }

    private void NormalizeAndValidateProductOptions(ProductAdminViewModel model)
    {
        model.ProductOptions ??= new();

        foreach (var option in model.ProductOptions)
        {
            option.ProductValues ??= new();
            option.ProductValues = option.ProductValues
                .Where(value => !string.IsNullOrWhiteSpace(value.ValueName))
                .ToList();
        }

        foreach (var requiredOption in new[] { "Size", "Nhân bánh" })
        {
            var option = model.ProductOptions.FirstOrDefault(item =>
                string.Equals(item.OptionName, requiredOption, StringComparison.OrdinalIgnoreCase));

            if (option is null || option.ProductValues.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.ProductOptions),
                    $"Vui lòng thêm ít nhất một giá trị cho {requiredOption}.");
            }
        }
    }

    private static List<ProductOptionAdminViewModel> CreateDefaultProductOptions()
    {
        return
        [
            new()
            {
                OptionName = "Size",
                ProductValues = [new()]
            },
            new()
            {
                OptionName = "Nhân bánh",
                ProductValues = [new()]
            }
        ];
    }

    private static void EnsureRequiredProductOptions(ProductAdminViewModel model)
    {
        foreach (var defaultOption in CreateDefaultProductOptions())
        {
            if (!model.ProductOptions.Any(option =>
                string.Equals(option.OptionName, defaultOption.OptionName, StringComparison.OrdinalIgnoreCase)))
            {
                model.ProductOptions.Add(defaultOption);
            }
        }

        model.ProductOptions = model.ProductOptions
            .OrderBy(option => option.OptionName == "Size" ? 0 : 1)
            .ToList();
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

        try
        {
            model.Image = await _imageStorageService.UploadProductImageAsync(model.ImageFile, HttpContext.RequestAborted);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), $"Khong the upload anh len Cloudinary: {ex.Message}");
        }
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
