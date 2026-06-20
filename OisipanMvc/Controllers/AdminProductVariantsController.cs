using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProductVariantsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminProductVariantsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index(int? productId)
    {
        var endpoint = productId.HasValue
            ? $"api/productvariants?productId={productId.Value}"
            : "api/productvariants";

        var variants = await Api.GetFromJsonAsync<List<ProductVariantAdminViewModel>>(endpoint) ?? new();
        ViewBag.Products = await GetProductItems(productId);
        ViewBag.SelectedProductId = productId;
        return View(variants);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? productId)
    {
        var model = new ProductVariantAdminViewModel
        {
            ProductId = productId ?? 0,
            IsActive = true
        };
        await LoadProducts(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductVariantAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadProducts(model);
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/productvariants", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            await LoadProducts(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Thêm biến thể thành công.";
        return RedirectToAction(nameof(Index), new { productId = model.ProductId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await Api.GetFromJsonAsync<ProductVariantAdminViewModel>($"api/productvariants/{id}");
        if (model is null)
        {
            return NotFound();
        }

        await LoadProducts(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductVariantAdminViewModel model)
    {
        if (id != model.ProductVariantId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadProducts(model);
            return View(model);
        }

        var response = await Api.PutAsJsonAsync($"api/productvariants/{id}", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            await LoadProducts(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật biến thể thành công.";
        return RedirectToAction(nameof(Index), new { productId = model.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? productId)
    {
        var response = await Api.DeleteAsync($"api/productvariants/{id}");
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            TempData["ErrorMessage"] = string.Join(
                " ",
                ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage));
        }
        else
        {
            TempData["SuccessMessage"] = "Xóa biến thể thành công.";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private static object ToRequest(ProductVariantAdminViewModel model)
    {
        return new
        {
            model.ProductVariantId,
            model.ProductId,
            model.Size,
            model.Filling,
            model.AdditionalPrice,
            model.Quantity,
            model.IsActive
        };
    }

    private async Task LoadProducts(ProductVariantAdminViewModel model)
    {
        model.Products = await GetProductItems(model.ProductId);
    }

    private async Task<List<SelectListItem>> GetProductItems(int? selectedProductId)
    {
        var products = await Api.GetFromJsonAsync<List<ProductAdminViewModel>>("api/products") ?? new();
        return products
            .OrderBy(product => product.Name)
            .Select(product => new SelectListItem(
                product.Name,
                product.ProductId.ToString(),
                product.ProductId == selectedProductId))
            .ToList();
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
