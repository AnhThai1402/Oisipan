using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using FrontendMvc.Models;

namespace FrontendMvc.Controllers;

public class ProductsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Details(int id)
    {
        var client = _httpClientFactory.CreateClient();
        var apiUrl = $"http://localhost:5188/api/products/{id}";
        
        try
        {
            var product = await client.GetFromJsonAsync<ProductCatalogViewModel>(apiUrl);
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
}
