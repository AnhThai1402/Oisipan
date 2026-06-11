using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly OishipanContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();

    public AccountsController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAll()
    {
        var accounts = await _context.Accounts
            .Include(a => a.Orders)
            .OrderByDescending(a => a.Role == "Admin")
            .ThenBy(a => a.FullName)
            .Select(a => new AccountResponse
            {
                UserId = a.UserId,
                FullName = a.FullName,
                Email = a.Email,
                PhoneNumber = a.PhoneNumber,
                Role = a.Role,
                Address = a.Address,
                Status = a.Status,
                OrderCount = a.Orders.Count
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountResponse>> GetById(int id)
    {
        var account = await _context.Accounts
            .Include(a => a.Orders)
            .FirstOrDefaultAsync(a => a.UserId == id);

        return account is null
            ? NotFound(new { message = "Không tìm thấy người dùng." })
            : Ok(ToResponse(account));
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(AccountCreateRequest request)
    {
        Normalize(request);
        RejectAdminRole(request.Role);
        await ValidateUnique(request.Email, request.PhoneNumber);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var account = new Account
        {
            FullName = request.FullName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = "User",
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Status = request.Status
        };

        account.Password = _passwordHasher.HashPassword(account, request.Password);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = account.UserId }, ToResponse(account));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AccountUpdateRequest request)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        if (IsAdmin(account))
        {
            return BadRequest(new { message = "Không thể chỉnh sửa tài khoản quản trị viên." });
        }

        Normalize(request);
        RejectAdminRole(request.Role);
        await ValidateUnique(request.Email, request.PhoneNumber, id);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        account.FullName = request.FullName.Trim();
        account.Email = request.Email;
        account.PhoneNumber = request.PhoneNumber;
        account.Role = "User";
        account.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        account.Status = request.Status;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            account.Password = _passwordHasher.HashPassword(account, request.NewPassword);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await _context.Accounts
            .Include(a => a.Orders)
            .FirstOrDefaultAsync(a => a.UserId == id);

        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        if (IsAdmin(account))
        {
            return BadRequest(new { message = "Không thể xóa tài khoản quản trị viên." });
        }

        if (account.Orders.Any())
        {
            return BadRequest(new { message = "Không thể xóa người dùng đã có đơn hàng. Bạn có thể khóa tài khoản thay thế." });
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task ValidateUnique(string email, string phoneNumber, int? excludeUserId = null)
    {
        if (await _context.Accounts.AnyAsync(a => a.Email == email && a.UserId != excludeUserId))
        {
            ModelState.AddModelError(nameof(AccountCreateRequest.Email), "Email đã được sử dụng.");
        }

        if (await _context.Accounts.AnyAsync(a => a.PhoneNumber == phoneNumber && a.UserId != excludeUserId))
        {
            ModelState.AddModelError(nameof(AccountCreateRequest.PhoneNumber), "Số điện thoại đã được sử dụng.");
        }
    }

    private void RejectAdminRole(string role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(AccountCreateRequest.Role), "Không thể cấp quyền quản trị viên cho tài khoản khác.");
        }
    }

    private static bool IsAdmin(Account account)
    {
        return string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private static void Normalize(AccountCreateRequest request)
    {
        request.Email = request.Email.Trim().ToLowerInvariant();
        request.PhoneNumber = request.PhoneNumber.Trim();
    }

    private static void Normalize(AccountUpdateRequest request)
    {
        request.Email = request.Email.Trim().ToLowerInvariant();
        request.PhoneNumber = request.PhoneNumber.Trim();
    }

    private static AccountResponse ToResponse(Account account)
    {
        return new AccountResponse
        {
            UserId = account.UserId,
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            Role = account.Role,
            Address = account.Address,
            Status = account.Status,
            OrderCount = account.Orders.Count
        };
    }
}
