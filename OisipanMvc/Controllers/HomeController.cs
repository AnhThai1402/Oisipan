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
        return View(await BuildStorefrontModel(null, null));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public async Task<IActionResult> Menu([FromQuery] Guid? categoryId = null, [FromQuery] int page = 1)
    {
        return View(await BuildStorefrontModel(categoryId, page));
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

    private async Task<StorefrontViewModel> BuildStorefrontModel(Guid? categoryId = null, int? page = 1)
    {
        var productUrl = categoryId.HasValue ? $"api/products?categoryId={categoryId.Value}" : "api/products";
        var productsTask = Api.GetFromJsonAsyncWithOptions<List<ProductCatalogViewModel>>(productUrl);
        var categoriesTask = Api.GetFromJsonAsyncWithOptions<List<CategoryAdminViewModel>>("api/categories");
        
        await Task.WhenAll(productsTask, categoriesTask);

        var products = await productsTask ?? new List<ProductCatalogViewModel>();
        var categories = await categoriesTask ?? new List<CategoryAdminViewModel>();

        var filteredProducts = products.Where(product => product.StockQuantity > 0).ToList();
        int totalItems = filteredProducts.Count;
        int totalPages = 1;
        var pagedProducts = filteredProducts;

        if (page.HasValue)
        {
            int pageSize = 12;
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            if (page.Value < 1) page = 1;
            if (page.Value > totalPages && totalPages > 0) page = totalPages;

            pagedProducts = filteredProducts.Skip((page.Value - 1) * pageSize).Take(pageSize).ToList();
        }

        return new StorefrontViewModel
        {
            Products = pagedProducts,
            Categories = categories,
            SelectedCategoryId = categoryId,
            CurrentPage = page ?? 1,
            TotalPages = totalPages
        };
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
