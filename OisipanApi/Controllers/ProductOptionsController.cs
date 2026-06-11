using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductOptionsController : ControllerBase
{
    private readonly OishipanContext _context;

    public ProductOptionsController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductOptionResponse>>> GetAll()
    {
        var options = await _context.ProductOptions
            .Include(po => po.Product)
            .Include(po => po.ProductValues)
            .OrderBy(po => po.Product == null ? string.Empty : po.Product.Name)
            .ThenBy(po => po.OptionName)
            .Select(po => ToResponse(po))
            .ToListAsync();

        return Ok(options);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductOptionResponse>> GetById(int id)
    {
        var option = await _context.ProductOptions
            .Include(po => po.Product)
            .Include(po => po.ProductValues)
            .FirstOrDefaultAsync(po => po.ProductOptionId == id);

        return option is null ? NotFound(new { message = "Khong tim thay tuy chon san pham." }) : Ok(ToResponse(option));
    }

    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<IEnumerable<ProductOptionResponse>>> GetByProductId(int productId)
    {
        if (!await _context.Products.AnyAsync(p => p.ProductId == productId))
        {
            return NotFound(new { message = "Khong tim thay san pham." });
        }

        var options = await _context.ProductOptions
            .Include(po => po.Product)
            .Include(po => po.ProductValues)
            .Where(po => po.ProductId == productId)
            .OrderBy(po => po.OptionName)
            .Select(po => ToResponse(po))
            .ToListAsync();

        return Ok(options);
    }

    [HttpPost]
    public async Task<ActionResult<ProductOptionResponse>> Create(ProductOptionRequest request)
    {
        if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
        {
            ModelState.AddModelError(nameof(request.ProductId), "San pham khong ton tai.");
        }

        var optionName = request.OptionName.Trim();
        if (await _context.ProductOptions.AnyAsync(po => po.ProductId == request.ProductId && po.OptionName == optionName))
        {
            ModelState.AddModelError(nameof(request.OptionName), "Tuy chon nay da ton tai trong san pham.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var option = new ProductOption
        {
            ProductId = request.ProductId,
            OptionName = optionName
        };

        _context.ProductOptions.Add(option);
        await _context.SaveChangesAsync();

        await _context.Entry(option).Reference(po => po.Product).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = option.ProductOptionId }, ToResponse(option));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductOptionRequest request)
    {
        var option = await _context.ProductOptions.FindAsync(id);
        if (option is null)
        {
            return NotFound(new { message = "Khong tim thay tuy chon san pham." });
        }

        if (!await _context.Products.AnyAsync(p => p.ProductId == request.ProductId))
        {
            ModelState.AddModelError(nameof(request.ProductId), "San pham khong ton tai.");
        }

        var optionName = request.OptionName.Trim();
        if (await _context.ProductOptions.AnyAsync(po =>
            po.ProductOptionId != id &&
            po.ProductId == request.ProductId &&
            po.OptionName == optionName))
        {
            ModelState.AddModelError(nameof(request.OptionName), "Tuy chon nay da ton tai trong san pham.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        option.ProductId = request.ProductId;
        option.OptionName = optionName;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var option = await _context.ProductOptions
            .Include(po => po.ProductValues)
            .FirstOrDefaultAsync(po => po.ProductOptionId == id);

        if (option is null)
        {
            return NotFound(new { message = "Khong tim thay tuy chon san pham." });
        }

        _context.ProductOptions.Remove(option);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ProductOptionResponse ToResponse(ProductOption option)
    {
        return new ProductOptionResponse
        {
            ProductOptionId = option.ProductOptionId,
            ProductId = option.ProductId,
            ProductName = option.Product?.Name,
            OptionName = option.OptionName,
            ProductValues = option.ProductValues
                .OrderBy(pv => pv.ValueName)
                .Select(pv => new ProductValueResponse
                {
                    ProductValueId = pv.ProductValueId,
                    ProductOptionId = pv.ProductOptionId,
                    ValueName = pv.ValueName,
                    AdditionalPrice = pv.AdditionalPrice
                })
                .ToList()
        };
    }
}
