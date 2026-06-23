using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VouchersController : ControllerBase
{
    private readonly OishipanContext _context;

    public VouchersController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VoucherResponse>>> GetAll()
    {
        var vouchers = await _context.Vouchers
            .OrderByDescending(v => v.CreatedDate)
            .Select(v => ToResponse(v))
            .ToListAsync();

        return Ok(vouchers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VoucherResponse>> GetById(int id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);

        return voucher is null ? NotFound(new { message = "Không tìm thấy voucher." }) : Ok(ToResponse(voucher));
    }

    [HttpPost]
    public async Task<ActionResult<VoucherResponse>> Create(VoucherRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Check if code already exists
        var existingVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == request.Code);
        if (existingVoucher is not null)
        {
            ModelState.AddModelError(nameof(request.Code), "Mã voucher này đã tồn tại.");
            return ValidationProblem(ModelState);
        }

        var voucher = new Voucher
        {
            Code = request.Code.Trim().ToUpper(),
            DiscountValue = request.DiscountValue,
            MinimumItems = request.MinimumItems,
            ExpiryDate = request.ExpiryDate,
            CreatedDate = DateTime.Now
        };

        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = voucher.VoucherId }, ToResponse(voucher));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VoucherRequest request)
    {
        var voucher = await _context.Vouchers.FindAsync(id);

        if (voucher is null)
        {
            return NotFound(new { message = "Không tìm thấy voucher." });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Check if new code is already used by another voucher
        if (voucher.Code != request.Code && await _context.Vouchers.AnyAsync(v => v.Code == request.Code))
        {
            ModelState.AddModelError(nameof(request.Code), "Mã voucher này đã tồn tại.");
            return ValidationProblem(ModelState);
        }

        voucher.Code = request.Code.Trim().ToUpper();
        voucher.DiscountValue = request.DiscountValue;
        voucher.MinimumItems = request.MinimumItems;
        voucher.ExpiryDate = request.ExpiryDate;

        _context.Vouchers.Update(voucher);
        await _context.SaveChangesAsync();

        return Ok(ToResponse(voucher));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);

        if (voucher is null)
        {
            return NotFound(new { message = "Không tìm thấy voucher." });
        }

        _context.Vouchers.Remove(voucher);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Voucher đã được xóa thành công." });
    }

    private static VoucherResponse ToResponse(Voucher voucher) => new()
    {
        VoucherId = voucher.VoucherId,
        Code = voucher.Code,
        DiscountValue = voucher.DiscountValue,
        ExpiryDate = voucher.ExpiryDate,
        CreatedDate = voucher.CreatedDate
    };
}
