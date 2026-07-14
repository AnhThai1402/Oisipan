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
            .Include(v => v.ProductVariantValues)
                .ThenInclude(pvv => pvv.ProductValue)
                    .ThenInclude(pv => pv!.ProductOption)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(variant => variant.ProductId == productId.Value);
        }

        var variantsData = await query
            .OrderBy(variant => variant.Product == null ? string.Empty : variant.Product.Name)
            .ToListAsync();

        var variants = variantsData.Select(variant => ToResponse(variant)).ToList();

        return Ok(variants);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductVariantResponse>> GetById(int id)
    {
        var variant = await _context.ProductVariants
            .Include(item => item.Product)
            .Include(v => v.ProductVariantValues)
                .ThenInclude(pvv => pvv.ProductValue)
                    .ThenInclude(pv => pv!.ProductOption)
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
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Status = request.IsActive ? "Active" : "Inactive",
            Sku = request.Sku
        };

        if (request.ProductValueIds != null)
        {
            foreach (var valueId in request.ProductValueIds)
            {
                variant.ProductVariantValues.Add(new ProductVariantValue { ProductValueId = valueId });
            }
        }

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        await UpdateProductQuantity(request.ProductId);
        
        var savedVariant = await _context.ProductVariants
            .Include(item => item.Product)
            .Include(v => v.ProductVariantValues)
                .ThenInclude(pvv => pvv.ProductValue)
                    .ThenInclude(pv => pv!.ProductOption)
            .FirstAsync(item => item.ProductVariantId == variant.ProductVariantId);

        return CreatedAtAction(nameof(GetById), new { id = variant.ProductVariantId }, ToResponse(savedVariant));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductVariantManageRequest request)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.ProductVariantValues)
            .FirstOrDefaultAsync(v => v.ProductVariantId == id);
            
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
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.Status = request.IsActive ? "Active" : "Inactive";
        variant.Sku = request.Sku;

        // Update ProductVariantValues
        _context.ProductVariantValues.RemoveRange(variant.ProductVariantValues);
        variant.ProductVariantValues.Clear();
        if (request.ProductValueIds != null)
        {
            foreach (var valueId in request.ProductValueIds)
            {
                variant.ProductVariantValues.Add(new ProductVariantValue { ProductValueId = valueId });
            }
        }

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

        var requestValues = request.ProductValueIds?.OrderBy(x => x).ToList() ?? new List<int>();
        var otherVariants = await _context.ProductVariants
            .Include(v => v.ProductVariantValues)
            .Where(v => v.ProductVariantId != currentId && v.ProductId == request.ProductId)
            .ToListAsync();
            
        foreach (var other in otherVariants)
        {
            var otherValues = other.ProductVariantValues.Select(pvv => pvv.ProductValueId).OrderBy(x => x).ToList();
            if (requestValues.SequenceEqual(otherValues))
            {
                ModelState.AddModelError(
                    nameof(request.ProductValueIds),
                    "Tổ hợp giá trị biến thể này đã tồn tại trong sản phẩm.");
                break;
            }
        }
    }

    private async Task UpdateProductQuantity(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product is null)
        {
            return;
        }

        product.StockQuantity = await _context.ProductVariants
            .Where(variant => variant.ProductId == productId)
            .SumAsync(variant => variant.StockQuantity);
        await _context.SaveChangesAsync();
    }

    private static ProductVariantResponse ToResponse(ProductVariant variant)
    {
        return new ProductVariantResponse
        {
            ProductVariantId = variant.ProductVariantId,
            ProductId = variant.ProductId,
            ProductName = variant.Product?.Name,
            Price = variant.Price,
            StockQuantity = variant.StockQuantity,
            Sku = variant.Sku,
            IsActive = variant.Status == "Active",
            VariantValues = variant.ProductVariantValues
                .Where(pvv => pvv.ProductValue != null && pvv.ProductValue.ProductOption != null)
                .Select(pvv => new ProductVariantValueResponse
                {
                    ProductOptionId = pvv.ProductValue!.ProductOptionId,
                    OptionName = pvv.ProductValue.ProductOption!.OptionName,
                    ProductValueId = pvv.ProductValueId,
                    ValueName = pvv.ProductValue.ValueName
                }).ToList()
        };
    }
}
