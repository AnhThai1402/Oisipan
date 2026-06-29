using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FrontendMvc.Services.IImageStorageService _imageStorageService;

    public AccountController(IHttpClientFactory httpClientFactory, FrontendMvc.Services.IImageStorageService imageStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/auth/register", model);
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        var account = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Không đọc được thông tin tài khoản.");
            return View(model);
        }

        await SignIn(account, rememberMe: false);
        TempData["CartMessage"] = "Đăng ký thành công.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/auth/login", model);
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        var account = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Không đọc được thông tin đăng nhập.");
            return View(model);
        }

        await SignIn(account, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("OisipanCookie");
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            await HttpContext.SignOutAsync("OisipanCookie");
            return RedirectToAction(nameof(Login));
        }

        try
        {
            var profile = await Api.GetFromJsonAsync<UserAdminViewModel>($"api/accounts/{userId}");
            if (profile is null) return NotFound();

            var addresses = await Api.GetFromJsonAsync<List<UserAddressViewModel>>($"api/accounts/{userId}/addresses");
            if (addresses != null)
            {
                profile.Addresses = addresses;
            }

            return View(profile);
        }
        catch (HttpRequestException)
        {
            TempData["CartError"] = "Không thể tải hồ sơ. Vui lòng thử lại.";
            return RedirectToAction("Index", "Home");
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetCheckoutProfile()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId)) return Unauthorized();

        try
        {
            var profile = await Api.GetFromJsonAsync<UserAdminViewModel>($"api/accounts/{userId}");
            var addresses = await Api.GetFromJsonAsync<List<UserAddressViewModel>>($"api/accounts/{userId}/addresses");
            
            return Json(new { 
                fullName = profile?.FullName, 
                phoneNumber = profile?.PhoneNumber, 
                addresses = addresses 
            });
        }
        catch
        {
            return BadRequest();
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        try
        {
            var profile = await Api.GetFromJsonAsync<UserAdminViewModel>($"api/accounts/{userId}");
            if (profile is null) return NotFound();

            var model = new ProfileUpdateViewModel
            {
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Email = profile.Email,
                AvatarUrl = profile.AvatarUrl
            };

            return View(model);
        }
        catch (HttpRequestException)
        {
            TempData["CartError"] = "Không thể tải hồ sơ. Vui lòng thử lại.";
            return RedirectToAction("Profile");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(ProfileUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        if (model.Avatar != null && model.Avatar.Length > 0)
        {
            var uploadResult = await _imageStorageService.UploadAvatarImageAsync(model.Avatar);
            model.AvatarUrl = uploadResult;
        }

        var response = await Api.PatchAsJsonAsync($"api/accounts/{userId}/profile", model);
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        // Update cookie claims if Email or FullName changed
        var currentPrincipal = User;
        if (currentPrincipal.Identity is ClaimsIdentity identity)
        {
            var nameClaim = identity.FindFirst(ClaimTypes.Name);
            if (nameClaim != null) identity.RemoveClaim(nameClaim);
            identity.AddClaim(new Claim(ClaimTypes.Name, model.FullName));

            var emailClaim = identity.FindFirst(ClaimTypes.Email);
            if (emailClaim != null) identity.RemoveClaim(emailClaim);
            identity.AddClaim(new Claim(ClaimTypes.Email, model.Email));

            await HttpContext.SignInAsync("OisipanCookie", new ClaimsPrincipal(identity));
        }

        TempData["CartMessage"] = "Cập nhật hồ sơ thành công.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress([FromForm] string province, [FromForm] string ward, [FromForm] string detail)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(ward) || string.IsNullOrWhiteSpace(detail))
        {
            TempData["CartError"] = "Vui lòng điền đầy đủ thông tin địa chỉ.";
            return RedirectToAction(nameof(Profile));
        }

        var fullAddress = $"{detail.Trim()}, {ward}, {province}";

        var request = new UserAddressCreateViewModel
        {
            FullAddress = fullAddress,
            IsDefault = false
        };

        var response = await Api.PostAsJsonAsync($"api/accounts/{userId}/addresses", request);
        if (!response.IsSuccessStatusCode)
        {
            TempData["CartError"] = "Không thể thêm địa chỉ mới. Vui lòng thử lại.";
        }
        else
        {
            TempData["CartMessage"] = "Thêm địa chỉ thành công.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var response = await Api.DeleteAsync($"api/accounts/{userId}/addresses/{addressId}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["CartError"] = "Không thể xóa địa chỉ. Vui lòng thử lại.";
        }
        else
        {
            TempData["CartMessage"] = "Đã xóa địa chỉ thành công.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.PostAsJsonAsync("api/auth/forgot-password", model);
        if (!response.IsSuccessStatusCode)
        {
            await AddApiErrors(response);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");

    private async Task SignIn(AuthResponse account, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.UserId.ToString()),
            new(ClaimTypes.Name, account.FullName),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.Role)
        };

        var identity = new ClaimsIdentity(claims, "OisipanCookie");
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await HttpContext.SignInAsync("OisipanCookie", principal, properties);
    }

    private async Task AddApiErrors(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
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
