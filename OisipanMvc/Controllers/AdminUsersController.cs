using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminUsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var users = await Api.GetFromJsonAsync<List<UserAdminViewModel>>("api/accounts") ?? new();
        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserAdminViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Vui lòng nhập mật khẩu.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/accounts", new
        {
            model.FullName,
            model.Email,
            model.PhoneNumber,
            model.Password,
            model.Role,
            model.Address,
            model.Status
        });

        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        TempData["SuccessMessage"] = "Thêm người dùng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await Api.GetFromJsonAsync<UserAdminViewModel>($"api/accounts/{id}");
        return user is null ? NotFound() : View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserAdminViewModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PutAsJsonAsync($"api/accounts/{id}", new
        {
            model.FullName,
            model.Email,
            model.PhoneNumber,
            model.Role,
            model.NewPassword,
            model.Address,
            model.Status
        });

        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật người dùng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id.ToString())
        {
            TempData["ErrorMessage"] = "Bạn không thể xóa chính tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        var response = await Api.DeleteAsync($"api/accounts/{id}");
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Xóa người dùng thành công.";
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
