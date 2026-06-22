using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly OishipanContext _context;

    public CategoriesController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll()
    {
        var categories = await _context.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Image = c.Image,
                ProductCount = c.Products.Count
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        return category is null ? NotFound(new { message = "Không tìm thấy danh mục." }) : Ok(ToResponse(category));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        var category = new Category
        {
            CategoryName = request.CategoryName.Trim(),
            Image = string.IsNullOrWhiteSpace(request.Image) ? null : request.Image.Trim()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, ToResponse(category));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound(new { message = "Không tìm thấy danh mục." });
        }

        category.CategoryName = request.CategoryName.Trim();
        category.Image = string.IsNullOrWhiteSpace(request.Image) ? null : request.Image.Trim();
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
        {
            return NotFound(new { message = "Không tìm thấy danh mục." });
        }

        if (category.Products.Any())
        {
            return BadRequest(new { message = "Không thể xóa danh mục đang có sản phẩm." });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Image = category.Image,
            ProductCount = category.Products.Count
        };
    }
}
