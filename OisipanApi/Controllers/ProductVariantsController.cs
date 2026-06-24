using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductVariantsController : ControllerBase
{
    private readonly OishipanContext _context;

    public ProductVariantsController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductVariantResponse>>> GetAll([FromQuery] int? productId)
    {
        var query = _context.ProductVariants
            .Include(variant => variant.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(variant => variant.ProductId == productId.Value);
        }

        var variantsData = await query
            .OrderBy(variant => variant.Product == null ? string.Empty : variant.Product.Name)
            .ThenBy(variant => variant.Size)
            .ThenBy(variant => variant.Filling)
            .ToListAsync();

        var variants = variantsData.Select(variant => ToResponse(variant)).ToList();

        return Ok(variants);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductVariantResponse>> GetById(int id)
    {
        var variant = await _context.ProductVariants
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.ProductVariantId == id);

        return variant is null
            ? NotFound(new { message = "Không tìm thấy biến thể sản phẩm." })
            : Ok(ToResponse(variant));
    }

    [HttpPost]
    public async Task<ActionResult<ProductVariantResponse>> Create(ProductVariantManageRequest request)
    {
        await ValidateRequest(request);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var variant = new ProductVariant
        {
            ProductId = request.ProductId,
            Size = request.Size.Trim(),
            Filling = request.Filling.Trim(),
            AdditionalPrice = request.AdditionalPrice,
            Quantity = request.Quantity,
            IsActive = request.IsActive
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        await UpdateProductQuantity(request.ProductId);
        await _context.Entry(variant).Reference(item => item.Product).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = variant.ProductVariantId }, ToResponse(variant));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductVariantManageRequest request)
    {
        var variant = await _context.ProductVariants.FindAsync(id);
        if (variant is null)
        {
            return NotFound(new { message = "Không tìm thấy biến thể sản phẩm." });
        }

        await ValidateRequest(request, id);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var previousProductId = variant.ProductId;
        variant.ProductId = request.ProductId;
        variant.Size = request.Size.Trim();
        variant.Filling = request.Filling.Trim();
        variant.AdditionalPrice = request.AdditionalPrice;
        variant.Quantity = request.Quantity;
        variant.IsActive = request.IsActive;
        await _context.SaveChangesAsync();

        await UpdateProductQuantity(previousProductId);
        if (previousProductId != request.ProductId)
        {
            await UpdateProductQuantity(request.ProductId);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var variant = await _context.ProductVariants.FindAsync(id);
        if (variant is null)
        {
            return NotFound(new { message = "Không tìm thấy biến thể sản phẩm." });
        }

        var productId = variant.ProductId;
        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();
        await UpdateProductQuantity(productId);

        return NoContent();
    }

    private async Task ValidateRequest(ProductVariantManageRequest request, int? currentId = null)
    {
        if (!await _context.Products.AnyAsync(product => product.ProductId == request.ProductId))
        {
            ModelState.AddModelError(nameof(request.ProductId), "Sản phẩm không tồn tại.");
        }

        var size = request.Size.Trim();
        var filling = request.Filling.Trim();
        var duplicateExists = await _context.ProductVariants.AnyAsync(variant =>
            variant.ProductVariantId != currentId &&
            variant.ProductId == request.ProductId &&
            variant.Size == size &&
            variant.Filling == filling);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(request.Size),
                "Tổ hợp size và nhân bánh đã tồn tại trong sản phẩm.");
        }
    }

    private async Task UpdateProductQuantity(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product is null)
        {
            return;
        }

        product.Quantity = await _context.ProductVariants
            .Where(variant => variant.ProductId == productId)
            .SumAsync(variant => variant.Quantity);
        await _context.SaveChangesAsync();
    }

    private static ProductVariantResponse ToResponse(ProductVariant variant)
    {
        return new ProductVariantResponse
        {
            ProductVariantId = variant.ProductVariantId,
            ProductId = variant.ProductId,
            ProductName = variant.Product?.Name,
            Size = variant.Size,
            Filling = variant.Filling,
            AdditionalPrice = variant.AdditionalPrice,
            Quantity = variant.Quantity,
            IsActive = variant.IsActive
        };
    }
}
