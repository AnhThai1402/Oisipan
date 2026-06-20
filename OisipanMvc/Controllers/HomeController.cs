using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FrontendMvc.Models;
using System.Net.Http.Json;

namespace FrontendMvc.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Menu()
    {
        return View();
    }

    public async Task<IActionResult> News()
    {
        var articles = await Api.GetFromJsonAsync<List<NewsArticleViewModel>>("api/newsarticles") ?? new();
        return View(articles);
    }

    public async Task<IActionResult> NewsDetail(int id)
    {
        var response = await Api.GetAsync($"api/newsarticles/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var article = await response.Content.ReadFromJsonAsync<NewsArticleViewModel>();
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

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
