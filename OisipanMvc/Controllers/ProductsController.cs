using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using FrontendMvc.Extensions;
using FrontendMvc.Models;

namespace FrontendMvc.Controllers;

public class ProductsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var product = await Api.GetFromJsonAsyncWithOptions<ProductCatalogViewModel>($"api/products/{id}");
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
