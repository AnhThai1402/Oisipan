using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var users = await Api.GetFromJsonAsync<List<UserAdminViewModel>>("api/accounts") ?? new();
            return View(users);
        }
        catch (HttpRequestException)
        {
            TempData["Message"] = "Không kết nối được API người dùng.";
            return View(new List<UserAdminViewModel>());
        }
    }

    public IActionResult Create()
    {
        TempData["ErrorMessage"] = "Admin không được tạo tài khoản tại đây.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(UserAdminViewModel model)
    {
        TempData["ErrorMessage"] = "Admin không được tạo tài khoản tại đây.";
        return Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await GetUser(id);
        if (model is null)
        {
            TempData["Message"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        if (IsAdmin(model))
        {
            TempData["Message"] = "Không thể chỉnh sửa tài khoản quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

        return View("CreateEdit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserAdminViewModel model)
    {
        model.UserId = id;
        ModelState.Remove(nameof(UserAdminViewModel.Password));

        if (!ModelState.IsValid)
        {
            return View("CreateEdit", model);
        }

        var existing = await GetUser(id);
        if (existing is null)
        {
            TempData["Message"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        if (IsAdmin(existing))
        {
            TempData["Message"] = "Không thể chỉnh sửa tài khoản quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

        var response = await Api.PutAsJsonAsync($"api/accounts/{id}", model);
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View("CreateEdit", model);
        }

        TempData["Message"] = "Cập nhật tài khoản thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await GetUser(id);
        if (model is null)
        {
            TempData["Message"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await GetUser(id);
        if (existing is null)
        {
            TempData["Message"] = "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Index));
        }

        if (IsAdmin(existing))
        {
            TempData["Message"] = "Không thể xóa tài khoản quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

        var response = await Api.DeleteAsync($"api/accounts/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Message"] = await ReadApiMessage(response) ?? "Không thể xóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Message"] = "Xóa tài khoản thành công.";
        return RedirectToAction(nameof(Index));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private async Task<UserAdminViewModel?> GetUser(int id)
    {
        try
        {
            return await Api.GetFromJsonAsync<UserAdminViewModel>($"api/accounts/{id}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static bool IsAdmin(UserAdminViewModel user)
    {
        return string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private async Task AddApiErrors(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy người dùng.");
            return;
        }

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
                    foreach (var validationMessage in error.Value.EnumerateArray())
                    {
                        ModelState.AddModelError(error.Name, validationMessage.GetString() ?? "Dữ liệu không hợp lệ.");
                    }
                }

                return;
            }

            if (root.TryGetProperty("message", out var apiMessage))
            {
                ModelState.AddModelError(string.Empty, apiMessage.GetString() ?? "Có lỗi xảy ra.");
                return;
            }
        }
        catch (JsonException)
        {
        }

        ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
    }

    private static async Task<string?> ReadApiMessage(HttpResponseMessage response)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.TryGetProperty("message", out var message)
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
