using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly OishipanContext _context;

    public WishlistController(OishipanContext context) => _context = context;

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<IEnumerable<WishlistItemResponse>>> Get(Guid userId)
    {
        return Ok(await _context.WishlistItems.Where(w => w.UserId == userId).Include(w => w.Product)
            .OrderByDescending(w => w.CreatedAt).Select(w => new WishlistItemResponse
            {
                WishlistItemId = w.WishlistItemId, ProductId = w.ProductId,
                ProductName = w.Product!.Name, Price = w.Product.Price, Image = w.Product.Image,
                StockQuantity = w.Product.StockQuantity, AddedAt = w.CreatedAt
            }).ToListAsync());
    }

    [HttpPost("{userId:guid}/{productId:guid}")]
    public async Task<IActionResult> Add(Guid userId, Guid productId)
    {
        if (!await _context.Accounts.AnyAsync(a => a.UserId == userId) || !await _context.Products.AnyAsync(p => p.ProductId == productId))
            return NotFound(new { message = "Tài khoản hoặc sản phẩm không tồn tại." });
        if (await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId)) return Ok();
        _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId, CreatedAt = DateTime.Now });
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{userId:guid}/{productId:guid}")]
    public async Task<IActionResult> Remove(Guid userId, Guid productId)
    {
        var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (item is null) return NotFound();
        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}