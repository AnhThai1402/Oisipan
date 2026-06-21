using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductVariantsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductVariantsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index(int? productId)
    {
        var products = await GetProductItems(productId);
        productId ??= int.TryParse(products.FirstOrDefault()?.Value, out var firstProductId)
            ? firstProductId
            : null;

        var model = new ProductOptionManagementViewModel
        {
            SelectedProductId = productId,
            SelectedProductName = products.FirstOrDefault(item => item.Value == productId?.ToString())?.Text,
            Products = products
        };

        if (productId.HasValue)
        {
            model.Options = await Api.GetFromJsonAsync<List<ProductOptionAdminViewModel>>(
                $"api/productoptions/product/{productId.Value}") ?? new();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOptionValue(
        int productId,
        string optionName,
        string valueName,
        decimal additionalPrice)
    {
        if (string.IsNullOrWhiteSpace(valueName))
        {
            TempData["ErrorMessage"] = $"Vui lòng nhập giá trị cho {optionName}.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var options = await Api.GetFromJsonAsync<List<ProductOptionAdminViewModel>>(
            $"api/productoptions/product/{productId}") ?? new();
        var option = options.FirstOrDefault(item =>
            string.Equals(item.OptionName, optionName, StringComparison.OrdinalIgnoreCase));

        if (option is null)
        {
            var optionResponse = await Api.PostAsJsonAsync("api/productoptions", new
            {
                ProductId = productId,
                OptionName = optionName
            });

            if (!optionResponse.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không thể tạo nhóm biến thể.";
                return RedirectToAction(nameof(Index), new { productId });
            }

            option = await optionResponse.Content.ReadFromJsonAsync<ProductOptionAdminViewModel>();
        }

        var response = await Api.PostAsJsonAsync("api/productvalues", new
        {
            ProductOptionId = option!.ProductOptionId,
            ValueName = valueName.Trim(),
            AdditionalPrice = additionalPrice
        });

        TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccessStatusCode
                ? $"Đã thêm {valueName.Trim()} vào {optionName}."
                : await ReadApiError(response, "Không thể thêm giá trị biến thể.");

        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOptionValue(
        int productId,
        int productValueId,
        int productOptionId,
        string valueName,
        decimal additionalPrice)
    {
        var response = await Api.PutAsJsonAsync($"api/productvalues/{productValueId}", new
        {
            ProductOptionId = productOptionId,
            ValueName = valueName.Trim(),
            AdditionalPrice = additionalPrice
        });

        TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccessStatusCode
                ? "Cập nhật biến thể thành công."
                : await ReadApiError(response, "Không thể cập nhật biến thể.");

        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOptionValue(int productId, int productValueId)
    {
        var response = await Api.DeleteAsync($"api/productvalues/{productValueId}");
        TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccessStatusCode
                ? "Xóa giá trị biến thể thành công."
                : await ReadApiError(response, "Không thể xóa giá trị biến thể.");

        return RedirectToAction(nameof(Index), new { productId });
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private static async Task<string> ReadApiError(HttpResponseMessage response, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                return string.Join(
                    " ",
                    errors.EnumerateObject()
                        .SelectMany(error => error.Value.EnumerateArray())
                        .Select(message => message.GetString())
                        .Where(message => !string.IsNullOrWhiteSpace(message)));
            }

            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? fallback;
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
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

}
