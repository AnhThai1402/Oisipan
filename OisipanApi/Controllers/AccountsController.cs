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

    [HttpGet("{id}/addresses")]
    public async Task<IActionResult> GetAddresses(Guid id)
    {
        var addresses = await _context.UserAddresses
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.IsDefault)
            .Select(a => new UserAddressResponse
            {
                AddressId = a.AddressId,
                FullAddress = a.FullAddress,
                IsDefault = a.IsDefault
            })
            .ToListAsync();

        return Ok(addresses);
    }

    [HttpPost("{id}/addresses")]
    public async Task<IActionResult> CreateAddress(Guid id, [FromBody] UserAddressCreateRequest request)
    {
        var accountExists = await _context.Accounts.AnyAsync(a => a.UserId == id);
        if (!accountExists) return NotFound("Không tìm thấy tài khoản.");

        // If this is the first address, or IsDefault is true, set others to false
        var existingAddresses = await _context.UserAddresses.Where(a => a.UserId == id).ToListAsync();
        var isFirst = existingAddresses.Count == 0;

        if (request.IsDefault || isFirst)
        {
            foreach (var addr in existingAddresses)
            {
                addr.IsDefault = false;
            }
        }

        var newAddress = new UserAddress
        {
            UserId = id,
            FullAddress = request.FullAddress.Trim(),
            IsDefault = request.IsDefault || isFirst
        };

        _context.UserAddresses.Add(newAddress);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAddresses), new { id = id }, newAddress);
    }

    [HttpDelete("{id}/addresses/{addressId}")]
    public async Task<IActionResult> DeleteAddress(Guid id, Guid addressId)
    {
        var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == id && a.AddressId == addressId);
        if (address == null) return NotFound("Không tìm thấy địa chỉ.");

        _context.UserAddresses.Remove(address);
        await _context.SaveChangesAsync();

        // If deleted address was default, make another one default if possible
        if (address.IsDefault)
        {
            var firstRemaining = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == id);
            if (firstRemaining != null)
            {
                firstRemaining.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id)
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
            Status = true
        };

        account.Password = _passwordHasher.HashPassword(account, request.Password);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = account.UserId }, ToResponse(account));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AccountUpdateRequest request)
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

    [HttpPatch("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, UserProfileUpdateRequest request)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        if (await _context.Accounts.AnyAsync(a => a.PhoneNumber == request.PhoneNumber && a.UserId != id))
        {
            ModelState.AddModelError(nameof(UserProfileUpdateRequest.PhoneNumber), "Số điện thoại đã được sử dụng.");
            return ValidationProblem(ModelState);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (await _context.Accounts.AnyAsync(a => a.Email == request.Email && a.UserId != id))
        {
            ModelState.AddModelError(nameof(UserProfileUpdateRequest.Email), "Email đã được sử dụng.");
            return ValidationProblem(ModelState);
        }

        account.FullName = request.FullName.Trim();
        account.PhoneNumber = request.PhoneNumber.Trim();
        account.Email = request.Email.Trim().ToLowerInvariant();
        if (request.AvatarUrl != null)
        {
            account.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
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

    private async Task ValidateUnique(string email, string phoneNumber, Guid? excludeUserId = null)
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
            AvatarUrl = account.AvatarUrl,
            Status = account.Status,
            OrderCount = account.Orders.Count
        };
    }
}
