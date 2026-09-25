using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;
using Oishipan.Services;
using System.Security.Claims;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly OishipanContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthController(OishipanContext context, JwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        request.Email = request.Email.Trim().ToLowerInvariant();
        request.PhoneNumber = request.PhoneNumber.Trim();

        if (await _context.Accounts.AnyAsync(a => a.Email == request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Email đã được sử dụng.");
        }

        if (await _context.Accounts.AnyAsync(a => a.PhoneNumber == request.PhoneNumber))
        {
            ModelState.AddModelError(nameof(request.PhoneNumber), "Số điện thoại đã được sử dụng.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var account = new Account
        {
            FullName = request.FullName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Role = "User",
            Status = true,
            AuthProvider = "Local"
        };

        account.Password = _passwordHasher.HashPassword(account, request.Password);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(ToAuthResponse(account));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);

        if (account is null)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }

        if (string.IsNullOrWhiteSpace(account.Password))
        {
            return BadRequest(new { message = "Tài khoản này chưa thiết lập mật khẩu. Vui lòng đăng nhập bằng Google để thiết lập mật khẩu." });
        }

        if (!IsPasswordValid(account, request.Password))
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }

        if (!account.Status)
        {
            return BadRequest(new { message = "Tài khoản đã bị khóa." });
        }

        return Ok(ToAuthResponse(account));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.PhoneNumber.Trim();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email && a.PhoneNumber == phone);

        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản với email và số điện thoại này." });
        }

        if (!account.Status)
        {
            return BadRequest(new { message = "Tài khoản đã bị khóa." });
        }

        account.Password = _passwordHasher.HashPassword(account, request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    private bool IsPasswordValid(Account account, string password)
    {
        if (string.IsNullOrEmpty(account.Password))
        {
            return false;
        }

        try
        {
            var result = _passwordHasher.VerifyHashedPassword(account, account.Password, password);
            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded
                || account.Password == password;
        }
        catch (FormatException)
        {
            return account.Password == password;
        }
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<GoogleLoginResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        try
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            var googleId = payload.Subject;
            var email = payload.Email.ToLowerInvariant();
            var fullName = payload.Name;

            var account = await _context.Accounts.FirstOrDefaultAsync(a =>
                a.GoogleId == googleId || a.Email == email);

            if (account is null)
            {
                account = new Account
                {
                    FullName = fullName,
                    Email = email,
                    GoogleId = googleId,
                    AuthProvider = "Google",
                    Role = "User",
                    Status = true,
                    PhoneNumber = null,
                    Password = null
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                return Ok(new GoogleLoginResponse
                {
                    RequiresPasswordSetup = true,
                    SetupEmail = account.Email,
                    SetupFullName = account.FullName
                });
            }

            if (!account.Status)
            {
                return BadRequest(new { message = "Tài khoản đã bị khóa." });
            }

            var hasChanges = false;

            if (string.IsNullOrWhiteSpace(account.GoogleId))
            {
                account.GoogleId = googleId;
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(account.FullName) && !string.IsNullOrWhiteSpace(fullName))
            {
                account.FullName = fullName;
                hasChanges = true;
            }

            var hasPassword = !string.IsNullOrWhiteSpace(account.Password);
            var authProvider = ResolveAuthProvider(hasGoogle: true, hasPassword: hasPassword);
            if (!string.Equals(account.AuthProvider, authProvider, StringComparison.OrdinalIgnoreCase))
            {
                account.AuthProvider = authProvider;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            if (!hasPassword)
            {
                return Ok(new GoogleLoginResponse
                {
                    RequiresPasswordSetup = true,
                    SetupEmail = account.Email,
                    SetupFullName = account.FullName
                });
            }

            return Ok(new GoogleLoginResponse
            {
                RequiresPasswordSetup = false,
                Auth = ToAuthResponse(account)
            });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "Google token không hợp lệ." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Đăng nhập Google thất bại: " + ex.Message });
        }
    }

    [HttpPost("google-setup-password")]
    public async Task<ActionResult<AuthResponse>> SetupGooglePassword(GooglePasswordSetupRequest request)
    {
        try
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            var googleId = payload.Subject;
            var email = payload.Email.ToLowerInvariant();
            var fullName = payload.Name;

            var account = await _context.Accounts.FirstOrDefaultAsync(a =>
                a.GoogleId == googleId || a.Email == email);

            if (account is null)
            {
                account = new Account
                {
                    FullName = fullName,
                    Email = email,
                    GoogleId = googleId,
                    Role = "User",
                    Status = true,
                    PhoneNumber = null
                };

                _context.Accounts.Add(account);
            }

            if (!account.Status)
            {
                return BadRequest(new { message = "Tài khoản đã bị khóa." });
            }

            account.GoogleId = googleId;
            if (string.IsNullOrWhiteSpace(account.FullName) && !string.IsNullOrWhiteSpace(fullName))
            {
                account.FullName = fullName;
            }

            account.Password = _passwordHasher.HashPassword(account, request.Password);
            account.AuthProvider = ResolveAuthProvider(hasGoogle: true, hasPassword: true);

            await _context.SaveChangesAsync();

            return Ok(ToAuthResponse(account));
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "Google token không hợp lệ." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Thiết lập mật khẩu thất bại: " + ex.Message });
        }
    }

    private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
    {
        var googleClientId = _configuration["GoogleAuth:ClientId"];
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { googleClientId }
        };

        return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }

    private static string ResolveAuthProvider(bool hasGoogle, bool hasPassword)
    {
        if (hasGoogle && hasPassword)
        {
            return "Both";
        }

        if (hasGoogle)
        {
            return "Google";
        }

        return hasPassword ? "Local" : "Local";
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        var account = await _context.Accounts.FindAsync(userId);
        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        }

        if (!account.Status)
        {
            return BadRequest(new { message = "Tài khoản đã bị khóa." });
        }

        return Ok(ToAuthResponse(account));
    }

    private AuthResponse ToAuthResponse(Account account)
    {
        var token = _jwtService.GenerateToken(account);

        return new AuthResponse
        {
            UserId = account.UserId,
            FullName = account.FullName,
            Email = account.Email,
            Role = account.Role,
            Token = token,
            AuthProvider = account.AuthProvider
        };
    }
}
