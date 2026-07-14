using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly OishipanContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();

    public AuthController(OishipanContext context)
    {
        _context = context;
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
            Status = "Active"
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

        if (account is null || !IsPasswordValid(account, request.Password))
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }

        if (account.Status != "Active")
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

        if (account.Status != "Active")
        {
            return BadRequest(new { message = "Tài khoản đã bị khóa." });
        }

        account.Password = _passwordHasher.HashPassword(account, request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    private bool IsPasswordValid(Account account, string password)
    {
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

    private static AuthResponse ToAuthResponse(Account account)
    {
        return new AuthResponse
        {
            UserId = account.UserId,
            FullName = account.FullName,
            Email = account.Email,
            Role = account.Role
        };
    }
}
