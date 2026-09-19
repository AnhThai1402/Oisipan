using CloudinaryDotNet;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace FrontendMvc.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FrontendMvc.Services.IImageStorageService _imageStorageService;
    private readonly IConfiguration _configuration;

    public AccountController(IHttpClientFactory httpClientFactory, FrontendMvc.Services.IImageStorageService imageStorageService, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
        _configuration = configuration;
    }
    private readonly PasswordHasher<Account> _passwordHasher = new PasswordHasher<Account>();
    [HttpPost]
    public async Task<IActionResult> PhoneLoginCallback(
    [FromBody] NumPhoneModel request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Firebase IdToken không hợp lệ."
                });
            }

            // Gửi request sang OisipanApi
            var response = await Api.PostAsJsonAsync(
                "api/auth/phone-login",
                request
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                return StatusCode(
                    (int)response.StatusCode,
                    new
                    {
                        success = false,
                        message = error
                    }
                );
            }

            var authResponse =
                await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không nhận được thông tin tài khoản."
                });
            }

            // Tạo Cookie đăng nhập cho FrontendMvc
            var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                authResponse.UserId.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                authResponse.FullName ?? ""
            ),

            new Claim(
                ClaimTypes.Email,
                authResponse.Email ?? ""
            ),

            new Claim(
                ClaimTypes.MobilePhone,
                authResponse.PhoneNumber ?? ""
            ),

            new Claim(
                ClaimTypes.Role,
                authResponse.Role ?? "User"
            )
        };

            const string CookieScheme = "OisipanCookie";

            var identity = new ClaimsIdentity(
                claims,
                CookieScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieScheme,
                principal
            );

            return Ok(new
            {
                success = true,
                userId = authResponse.UserId,
                redirectUrl = Url.Action("Index", "Home")
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = "Đăng nhập bằng số điện thoại thất bại: "
                        + ex.Message
            });
        }
    }
    //[HttpPost]
    //public async Task<IActionResult> LoginPhone(
    //[FromBody] Models.NumPhoneModel request)
    //{
    //    try
    //    {
    //        var response = await Api.PostAsJsonAsync(
    //            "api/auth/phone-login",
    //            request
    //        );

    //        if (!response.IsSuccessStatusCode)
    //        {
    //            var error = await response.Content.ReadAsStringAsync();

    //            return StatusCode(
    //                (int)response.StatusCode,
    //                error
    //            );
    //        }

    //        var authResponse =
    //            await response.Content.ReadFromJsonAsync<AuthResponse>();

    //        if (authResponse == null)
    //        {
    //            return BadRequest(new
    //            {
    //                message = "API không trả về thông tin đăng nhập."
    //            });
    //        }

    //        var claims = new List<Claim>
    //    {
    //        new Claim(
    //            ClaimTypes.NameIdentifier,
    //            authResponse.UserId.ToString()
    //        ),

    //        new Claim(
    //            ClaimTypes.MobilePhone,
    //            authResponse.PhoneNumber ?? ""
    //        ),

    //        new Claim(
    //            ClaimTypes.Name,
    //            authResponse.FullName ?? ""
    //        ),

    //        new Claim(
    //            ClaimTypes.Role,
    //            authResponse.Role ?? "User"
    //        )
    //    };

    //        var identity = new ClaimsIdentity(
    //            claims,
    //            CookieAuthenticationDefaults.AuthenticationScheme
    //        );

    //        var principal = new ClaimsPrincipal(identity);

    //        await HttpContext.SignInAsync(
    //            CookieAuthenticationDefaults.AuthenticationScheme,
    //            principal
    //        );

    //        return Ok(new
    //        {
    //            success = true,
    //            userId = authResponse.UserId
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest(new
    //        {
    //            message = "Đăng nhập thất bại: " + ex.Message
    //        });
    //    }
    //}


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
        ViewBag.ApiKey = _configuration["Firebase:apiKey"];
        ViewBag.AuthDomain = _configuration["Firebase:authDomain"];
        ViewBag.ProjectId = _configuration["Firebase:projectId"];
        ViewBag.StorageBucket = _configuration["Firebase:storageBucket"];
        ViewBag.MessagingSenderId = _configuration["Firebase:messagingSenderId"];
        ViewBag.AppId = _configuration["Firebase:appId"];
        SetGoogleClientId();
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        SetGoogleClientId();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
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
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không kết nối được tới API. Hãy chạy BackendApi (http://localhost:5110) rồi thử lại.");
            return View(model);
        }
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
        if (!Guid.TryParse(userIdValue, out var userId))
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
        if (!Guid.TryParse(userIdValue, out var userId)) return Unauthorized();

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
        if (!Guid.TryParse(userIdValue, out var userId))
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
        if (!Guid.TryParse(userIdValue, out var userId))
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
        if (!Guid.TryParse(userIdValue, out var userId))
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
    public async Task<IActionResult> DeleteAddress(Guid addressId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
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

    [HttpPost]
    public async Task<IActionResult> GoogleLoginCallback([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.IdToken))
        {
            return Json(new { success = false, message = "Thiếu Google ID Token." });
        }

        try
        {
            var response = await Api.PostAsJsonAsync("api/auth/google-login", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ExtractErrorMessage(response);
                return Json(new { success = false, message = errorMessage });
            }

            var account = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (account is null)
            {
                return Json(new { success = false, message = "Không đọc được thông tin đăng nhập." });
            }

            await SignIn(account, rememberMe: true);

            var redirectUrl = string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                ? Url.Action("Index", "Admin")
                : Url.Action("Index", "Home");

            return Json(new { success = true, redirectUrl });
        }
        catch (HttpRequestException ex)
        {
            return Json(new { success = false, message = "Không thể kết nối tới máy chủ xác thực. Vui lòng kiểm tra Backend API đang chạy. Chi tiết: " + ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra khi đăng nhập bằng Google: " + ex.Message });
        }
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

    private void SetGoogleClientId()
    {
        ViewBag.GoogleClientId = _configuration["GoogleAuth:ClientId"];
    }

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

    private static async Task<string> ExtractErrorMessage(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "Google token không hợp lệ hoặc đã hết hạn.";
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Có lỗi xảy ra. Vui lòng thử lại.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var messageProperty))
            {
                return messageProperty.GetString() ?? "Có lỗi xảy ra.";
            }
        }
        catch (JsonException)
        {
        }

        return "Có lỗi xảy ra. Vui lòng thử lại.";
    }
}
