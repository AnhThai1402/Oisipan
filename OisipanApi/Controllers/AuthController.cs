using Google.Apis.Auth;
using FirebaseAdmin.Auth;
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
        if (!await _context.Database.CanConnectAsync())
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Dịch vụ cơ sở dữ liệu đang không khả dụng. Vui lòng kiểm tra SQL Server và chuỗi kết nối."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);

        if (account is null)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }

        if (account.AuthProvider != "Local")
        {
            return BadRequest(new { message = $"Tài khoản này được đăng ký bằng {account.AuthProvider}. Vui lòng sử dụng phương thức đăng nhập tương ứng." });
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
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        try
        {
            // Verify Google ID Token
            var googleClientId = _configuration["GoogleAuth:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

            // Extract user info từ Google payload
            var googleId = payload.Subject;
            var email = payload.Email.ToLowerInvariant();
            var fullName = payload.Name;

            // Tìm account theo GoogleId hoặc Email
            var account = await _context.Accounts.FirstOrDefaultAsync(a => 
                a.GoogleId == googleId || a.Email == email);

            if (account is null)
            {
                // Tạo account mới cho Google user
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
            }
            else
            {
                // Account đã tồn tại - cập nhật GoogleId nếu chưa có
                if (string.IsNullOrEmpty(account.GoogleId))
                {
                    account.GoogleId = googleId;
                    account.AuthProvider = "Google";
                    await _context.SaveChangesAsync();
                }

                // Check status
                if (!account.Status)
                {
                    return BadRequest(new { message = "Tài khoản đã bị khóa." });
                }
            }

            return Ok(ToAuthResponse(account));
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

    [HttpPost("phone-login")]
    public async Task<ActionResult<AuthResponse>> PhoneLogin(PhoneLoginRequest request)
    {
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
            
            if (!decodedToken.Claims.TryGetValue("phone_number", out var phoneObj))
            {
                return BadRequest(new { message = "Không lấy được số điện thoại từ token." });
            }
            
            string phone = phoneObj?.ToString() ?? "";
            
            // Format phone number to match our DB (+84901234567 -> 0901234567)
            if (phone.StartsWith("+84"))
            {
                phone = "0" + phone.Substring(3);
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.PhoneNumber == phone);

            if (account == null)
            {
                account = new Account
                {
                    FullName = "Người dùng mới",
                    PhoneNumber = phone,
                    Email = $"phone_{Guid.NewGuid():N}@local.com",
                    Role = "User",
                    Address = "",
                    Status = true,
                    AuthProvider = "Phone"
                };

                account.Password = _passwordHasher.HashPassword(account, Guid.NewGuid().ToString());
                
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (!account.Status)
                {
                    return BadRequest(new { message = "Tài khoản đã bị khóa." });
                }
            }

            return Ok(ToAuthResponse(account));
        }
        catch (FirebaseAuthException)
        {
            return Unauthorized(new { message = "Phone token không hợp lệ." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Đăng nhập bằng số điện thoại thất bại: " + ex.Message });
        }
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
