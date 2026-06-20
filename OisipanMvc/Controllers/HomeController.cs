using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using FrontendMvc.Models;

namespace FrontendMvc.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildStorefrontModel());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public async Task<IActionResult> Menu()
    {
        return View(await BuildStorefrontModel());
    }

    public IActionResult News()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<StorefrontViewModel> BuildStorefrontModel()
    {
        var products = await Api.GetFromJsonAsync<List<ProductCatalogViewModel>>("api/products")
            ?? new List<ProductCatalogViewModel>();

        return new StorefrontViewModel
        {
            Products = products.Where(product => product.Quantity > 0).ToList()
        };
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
