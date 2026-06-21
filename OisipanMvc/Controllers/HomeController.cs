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

    public async Task<IActionResult> ProductDetail(int id)
    {
        var response = await Api.GetAsync($"api/products/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? NotFound()
                : StatusCode((int)response.StatusCode);
        }

        var product = await response.Content.ReadFromJsonAsync<ProductCatalogViewModel>();
        if (product is null)
        {
            return NotFound();
        }

        product.ProductVariants = await Api.GetFromJsonAsync<List<ProductVariantCatalogViewModel>>(
            $"api/productvariants?productId={id}") ?? new();

        return View(product);
    }

    public async Task<IActionResult> News()
    {
        var articles = await Api.GetFromJsonAsync<List<NewsArticleViewModel>>("api/newsarticles")
            ?? new List<NewsArticleViewModel>();
        return View(articles);
    }

    public async Task<IActionResult> NewsDetail(int id)
    {
        var article = await Api.GetFromJsonAsync<NewsArticleViewModel>($"api/newsarticles/{id}");
        return article is null ? NotFound() : View(article);
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
