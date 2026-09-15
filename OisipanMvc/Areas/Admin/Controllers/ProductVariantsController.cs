using System.Net.Http.Json;
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

    public async Task<IActionResult> Index(Guid? productId)
    {
        var products = await GetProductItems(productId);
        productId ??= Guid.TryParse(products.FirstOrDefault()?.Value, out var firstProductId)
            ? firstProductId
            : null;

        var model = new ProductOptionManagementViewModel
        {
            SelectedProductId = productId,
            SelectedProductName = products.FirstOrDefault(item => item.Value == productId?.ToString())?.Text,
            Products = products
        };

        return View(model);
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private async Task<List<SelectListItem>> GetProductItems(Guid? selectedProductId)
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
