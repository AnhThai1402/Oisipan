using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminCategoriesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminCategoriesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await Api.GetFromJsonAsync<List<CategoryAdminViewModel>>("api/categories") ?? new();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/categories", new
        {
            model.CategoryName,
            model.Description
        });

        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        TempData["SuccessMessage"] = "Thêm danh mục thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await Api.GetFromJsonAsync<CategoryAdminViewModel>($"api/categories/{id}");
        return category is null ? NotFound() : View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryAdminViewModel model)
    {
        if (id != model.CategoryId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PutAsJsonAsync($"api/categories/{id}", new
        {
            model.CategoryName,
            model.Description
        });

        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật danh mục thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/categories/{id}");
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Xóa danh mục thành công.";
        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

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
