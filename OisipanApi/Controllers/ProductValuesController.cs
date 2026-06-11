using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductValuesController : ControllerBase
{
    private readonly OishipanContext _context;

    public ProductValuesController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductValueResponse>>> GetAll()
    {
        var values = await _context.ProductValues
            .OrderBy(pv => pv.ValueName)
            .Select(pv => ToResponse(pv))
            .ToListAsync();

        return Ok(values);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductValueResponse>> GetById(int id)
    {
        var value = await _context.ProductValues.FindAsync(id);

        return value is null ? NotFound(new { message = "Khong tim thay gia tri tuy chon." }) : Ok(ToResponse(value));
    }

    [HttpGet("option/{productOptionId:int}")]
    public async Task<ActionResult<IEnumerable<ProductValueResponse>>> GetByOptionId(int productOptionId)
    {
        if (!await _context.ProductOptions.AnyAsync(po => po.ProductOptionId == productOptionId))
        {
            return NotFound(new { message = "Khong tim thay tuy chon san pham." });
        }

        var values = await _context.ProductValues
            .Where(pv => pv.ProductOptionId == productOptionId)
            .OrderBy(pv => pv.ValueName)
            .Select(pv => ToResponse(pv))
            .ToListAsync();

        return Ok(values);
    }

    [HttpPost]
    public async Task<ActionResult<ProductValueResponse>> Create(ProductValueRequest request)
    {
        if (!await _context.ProductOptions.AnyAsync(po => po.ProductOptionId == request.ProductOptionId))
        {
            ModelState.AddModelError(nameof(request.ProductOptionId), "Tuy chon san pham khong ton tai.");
        }

        var valueName = request.ValueName.Trim();
        if (await _context.ProductValues.AnyAsync(pv => pv.ProductOptionId == request.ProductOptionId && pv.ValueName == valueName))
        {
            ModelState.AddModelError(nameof(request.ValueName), "Gia tri nay da ton tai trong tuy chon.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var value = new ProductValue
        {
            ProductOptionId = request.ProductOptionId,
            ValueName = valueName,
            AdditionalPrice = request.AdditionalPrice
        };

        _context.ProductValues.Add(value);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = value.ProductValueId }, ToResponse(value));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductValueRequest request)
    {
        var value = await _context.ProductValues.FindAsync(id);
        if (value is null)
        {
            return NotFound(new { message = "Khong tim thay gia tri tuy chon." });
        }

        if (!await _context.ProductOptions.AnyAsync(po => po.ProductOptionId == request.ProductOptionId))
        {
            ModelState.AddModelError(nameof(request.ProductOptionId), "Tuy chon san pham khong ton tai.");
        }

        var valueName = request.ValueName.Trim();
        if (await _context.ProductValues.AnyAsync(pv =>
            pv.ProductValueId != id &&
            pv.ProductOptionId == request.ProductOptionId &&
            pv.ValueName == valueName))
        {
            ModelState.AddModelError(nameof(request.ValueName), "Gia tri nay da ton tai trong tuy chon.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        value.ProductOptionId = request.ProductOptionId;
        value.ValueName = valueName;
        value.AdditionalPrice = request.AdditionalPrice;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var value = await _context.ProductValues.FindAsync(id);
        if (value is null)
        {
            return NotFound(new { message = "Khong tim thay gia tri tuy chon." });
        }

        _context.ProductValues.Remove(value);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ProductValueResponse ToResponse(ProductValue value)
    {
        return new ProductValueResponse
        {
            ProductValueId = value.ProductValueId,
            ProductOptionId = value.ProductOptionId,
            ValueName = value.ValueName,
            AdditionalPrice = value.AdditionalPrice
        };
    }
}
