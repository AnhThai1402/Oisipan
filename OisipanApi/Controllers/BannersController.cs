using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BannersController : ControllerBase
{
    private readonly OishipanContext _context;

    public BannersController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Banner>>> GetBanners([FromQuery] bool includeInactive = false)
    {
        var query = _context.Banners.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        return await query.OrderBy(b => b.DisplayOrder).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Banner>> GetBanner(Guid id)
    {
        var banner = await _context.Banners.FindAsync(id);

        if (banner == null)
        {
            return NotFound();
        }

        return banner;
    }

    [HttpPost]
    public async Task<ActionResult<Banner>> CreateBanner(Banner banner)
    {
        banner.BannerId = Guid.NewGuid();
        banner.CreatedAt = DateTime.Now;

        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBanner), new { id = banner.BannerId }, banner);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBanner(Guid id, Banner banner)
    {
        if (id != banner.BannerId)
        {
            return BadRequest();
        }

        var existingBanner = await _context.Banners.FindAsync(id);
        if (existingBanner == null)
        {
            return NotFound();
        }

        existingBanner.Title = banner.Title;
        existingBanner.ImageUrl = banner.ImageUrl;
        existingBanner.Link = banner.Link;
        existingBanner.IsActive = banner.IsActive;
        existingBanner.DisplayOrder = banner.DisplayOrder;
        existingBanner.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BannerExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBanner(Guid id)
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner == null)
        {
            return NotFound();
        }

        _context.Banners.Remove(banner);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool BannerExists(Guid id)
    {
        return _context.Banners.Any(e => e.BannerId == id);
    }
}
