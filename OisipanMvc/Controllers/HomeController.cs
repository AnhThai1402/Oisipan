using System.Diagnostics;
using System.Net.Http.Json;
using FrontendMvc.Extensions;
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

    public async Task<IActionResult> Menu([FromQuery] Guid? categoryId = null)
    {
        return View(await BuildStorefrontModel(categoryId));
    }

    public async Task<IActionResult> ProductDetail(Guid id)
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



    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<StorefrontViewModel> BuildStorefrontModel(Guid? categoryId = null)
    {
        var productUrl = categoryId.HasValue ? $"api/products?categoryId={categoryId.Value}" : "api/products";
        var productsTask = Api.GetFromJsonAsyncWithOptions<List<ProductCatalogViewModel>>(productUrl);
        var bannersTask = Api.GetFromJsonAsyncWithOptions<List<BannerViewModel>>("api/banners");
        var categoriesTask = Api.GetFromJsonAsyncWithOptions<List<CategoryAdminViewModel>>("api/categories");
        
        await Task.WhenAll(productsTask, bannersTask, categoriesTask);

        var products = await productsTask ?? new List<ProductCatalogViewModel>();
        var banners = await bannersTask ?? new List<BannerViewModel>();
        var categories = await categoriesTask ?? new List<CategoryAdminViewModel>();

        return new StorefrontViewModel
        {
            Products = products.Where(product => product.StockQuantity > 0).ToList(),
            Banners = banners,
            Categories = categories,
            SelectedCategoryId = categoryId
        };
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
