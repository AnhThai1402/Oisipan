using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly OishipanContext _context;

    public ProductsController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll([FromQuery] int? categoryId = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponse
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,
                Image = p.Image,
                Quantity = p.Quantity,
                CategoryId = p.CategoryId,
                CategoryName = p.Category == null ? null : p.Category.CategoryName,
                Description = p.Description,
                ProductOptions = p.ProductOptions
                    .OrderBy(po => po.OptionName)
                    .Select(po => new ProductOptionResponse
                    {
                        ProductOptionId = po.ProductOptionId,
                        ProductId = po.ProductId,
                        ProductName = p.Name,
                        OptionName = po.OptionName,
                        ProductValues = po.ProductValues
                            .OrderBy(pv => pv.ValueName)
                            .Select(pv => new ProductValueResponse
                            {
                                ProductValueId = pv.ProductValueId,
                                ProductOptionId = pv.ProductOptionId,
                                ValueName = pv.ValueName,
                                AdditionalPrice = pv.AdditionalPrice
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        return product is null ? NotFound(new { message = "Không tìm thấy sản phẩm." }) : Ok(ToResponse(product));
    }

    [HttpGet("admin/products")]
    public async Task<ActionResult<IEnumerable<AdminProductResponse>>> GetAllForAdmin([FromQuery] int? categoryId = null)
    {
        var query = _context.Products
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync();

        var result = products.Select(p =>
        {
            var categoryName = _context.Categories
                .Where(c => c.CategoryId == p.CategoryId)
                .Select(c => c.CategoryName)
                .FirstOrDefault();

            return ToAdminResponse(p, categoryName);
        }).ToList();

        return Ok(result);
    }

    [HttpGet("admin/{id:int}")]
    public async Task<ActionResult<AdminProductResponse>> GetByIdForAdmin(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        var categoryName = await _context.Categories
            .Where(c => c.CategoryId == product.CategoryId)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync();

        return Ok(ToAdminResponse(product, categoryName));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        if (!await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Danh mục không tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Image = string.IsNullOrWhiteSpace(request.Image) ? null : request.Image.Trim(),
            Quantity = request.Quantity,
            CategoryId = request.CategoryId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Sku = request.Name.Trim().ToUpper().Substring(0, Math.Min(30, request.Name.Trim().Length)).Replace(" ", "-"),
            MinimumStock = request.MinimumStock,
            Status = request.Status ?? "active",
            CreatedDate = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, ToResponse(product));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        if (!await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Danh mục không tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        product.Name = request.Name.Trim();
        product.Price = request.Price;
        product.Image = string.IsNullOrWhiteSpace(request.Image) ? null : request.Image.Trim();
        product.Quantity = request.Quantity;
        product.CategoryId = request.CategoryId;
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Status = request.Status?.Trim() ?? "active";
        product.MinimumStock = request.MinimumStock;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Price = product.Price,
            Image = product.Image,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.CategoryName,
            Description = product.Description,
            ProductOptions = product.ProductOptions
                .OrderBy(po => po.OptionName)
                .Select(po => new ProductOptionResponse
                {
                    ProductOptionId = po.ProductOptionId,
                    ProductId = po.ProductId,
                    ProductName = product.Name,
                    OptionName = po.OptionName,
                    ProductValues = po.ProductValues
                        .OrderBy(pv => pv.ValueName)
                        .Select(pv => new ProductValueResponse
                        {
                            ProductValueId = pv.ProductValueId,
                            ProductOptionId = pv.ProductOptionId,
                            ValueName = pv.ValueName,
                            AdditionalPrice = pv.AdditionalPrice
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static AdminProductResponse ToAdminResponse(Product product, string? categoryName = null)
    {
        return new AdminProductResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Price = product.Price,
            Image = product.Image,
            Quantity = product.Quantity,
            MinimumStock = product.MinimumStock,
            CategoryId = product.CategoryId,
            CategoryName = categoryName ?? product.Category?.CategoryName,
            Description = product.Description,
            Status = product.Status ?? "active",
            CreatedDate = product.CreatedDate,
            UpdatedDate = product.UpdatedDate,
            ProductOptions = product.ProductOptions
                .OrderBy(po => po.OptionName)
                .Select(po => new AdminProductOptionResponse
                {
                    ProductOptionId = po.ProductOptionId,
                    ProductId = po.ProductId,
                    ProductName = product.Name,
                    OptionName = po.OptionName,
                    ProductValues = po.ProductValues
                        .OrderBy(pv => pv.ValueName)
                        .Select(pv => new AdminProductValueResponse
                        {
                            ProductValueId = pv.ProductValueId,
                            ProductOptionId = pv.ProductOptionId,
                            ValueName = pv.ValueName,
                            AdditionalPrice = pv.AdditionalPrice
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
