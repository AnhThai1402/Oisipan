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
    private readonly IConfiguration _configuration;

    public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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
            ModelState.AddModelError(string.Empty, "KhÃ´ng Ä‘á»c Ä‘Æ°á»£c thÃ´ng tin tÃ i khoáº£n.");
            return View(model);
        }

        await SignIn(account, rememberMe: false);
        TempData["CartMessage"] = "ÄÄƒng kÃ½ thÃ nh cÃ´ng.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login()
    {
        ViewBag.GoogleClientId = _configuration["GoogleAuth:ClientId"];
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
            ModelState.AddModelError(string.Empty, "KhÃ´ng Ä‘á»c Ä‘Æ°á»£c thÃ´ng tin Ä‘Äƒng nháº­p.");
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
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            await HttpContext.SignOutAsync("OisipanCookie");
            return RedirectToAction(nameof(Login));
    [HttpPost]
    public async Task<IActionResult> GoogleLoginCallback([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.IdToken))
        {
            return Json(new { success = false, message = "Thiáº¿u Google ID Token." });
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
            TempData["CartError"] = "KhÃ´ng thá»ƒ táº£i há»“ sÆ¡. Vui lÃ²ng thá»­ láº¡i.";
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
            TempData["CartError"] = "KhÃ´ng thá»ƒ táº£i há»“ sÆ¡. Vui lÃ²ng thá»­ láº¡i.";
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

        TempData["CartMessage"] = "Cáº­p nháº­t há»“ sÆ¡ thÃ nh cÃ´ng.";
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
            TempData["CartError"] = "Vui lÃ²ng Ä‘iá»n Ä‘áº§y Ä‘á»§ thÃ´ng tin Ä‘á»‹a chá»‰.";
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
            TempData["CartError"] = "KhÃ´ng thá»ƒ thÃªm Ä‘á»‹a chá»‰ má»›i. Vui lÃ²ng thá»­ láº¡i.";
        }
        else
        {
            TempData["CartMessage"] = "ThÃªm Ä‘á»‹a chá»‰ thÃ nh cÃ´ng.";
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
            TempData["CartError"] = "KhÃ´ng thá»ƒ xÃ³a Ä‘á»‹a chá»‰. Vui lÃ²ng thá»­ láº¡i.";
        }
        else
        {
            TempData["CartMessage"] = "ÄÃ£ xÃ³a Ä‘á»‹a chá»‰ thÃ nh cÃ´ng.";
        }

        return RedirectToAction(nameof(Profile));
    }

            var response = await Api.PostAsJsonAsync("api/auth/google-login", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ExtractErrorMessage(response);
                return Json(new { success = false, message = errorMessage });
            }

            var account = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (account is null)
            {
                return Json(new { success = false, message = "KhÃ´ng Ä‘á»c Ä‘Æ°á»£c thÃ´ng tin Ä‘Äƒng nháº­p." });
            }

            await SignIn(account, rememberMe: true);

            var redirectUrl = string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                ? Url.Action("Index", "Admin")
                : Url.Action("Index", "Home");

            return Json(new { success = true, redirectUrl });
        }
        catch (HttpRequestException ex)
        {
            return Json(new { success = false, message = "KhÃ´ng thá»ƒ káº¿t ná»‘i tá»›i mÃ¡y chá»§ xÃ¡c thá»±c. Vui lÃ²ng kiá»ƒm tra Backend API Ä‘ang cháº¡y. Chi tiáº¿t: " + ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "CÃ³ lá»—i xáº£y ra khi Ä‘Äƒng nháº­p báº±ng Google: " + ex.Message });
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

        TempData["SuccessMessage"] = "Äáº·t láº¡i máº­t kháº©u thÃ nh cÃ´ng. Vui lÃ²ng Ä‘Äƒng nháº­p.";
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
            ModelState.AddModelError(string.Empty, "Email hoáº·c máº­t kháº©u khÃ´ng Ä‘Ãºng.");
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError(string.Empty, "CÃ³ lá»—i xáº£y ra. Vui lÃ²ng thá»­ láº¡i.");
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
                        ModelState.AddModelError(error.Name, message.GetString() ?? "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡.");
                    }
                }

                return;
            }

            if (root.TryGetProperty("message", out var messageProperty))
            {
                ModelState.AddModelError(string.Empty, messageProperty.GetString() ?? "CÃ³ lá»—i xáº£y ra.");
                return;
            }
        }
        catch (JsonException)
        {
        }

        ModelState.AddModelError(string.Empty, "CÃ³ lá»—i xáº£y ra. Vui lÃ²ng thá»­ láº¡i.");
    }

    private static async Task<string> ExtractErrorMessage(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "Google token khÃ´ng há»£p lá»‡ hoáº·c Ä‘Ã£ háº¿t háº¡n.";
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return "CÃ³ lá»—i xáº£y ra. Vui lÃ²ng thá»­ láº¡i.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var messageProperty))
            {
                return messageProperty.GetString() ?? "CÃ³ lá»—i xáº£y ra.";
            }
        }
        catch (JsonException)
        {
        }

        return "CÃ³ lá»—i xáº£y ra. Vui lÃ²ng thá»­ láº¡i.";
    }
}

