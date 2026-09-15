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
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll([FromQuery] Guid? categoryId = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .Include(p => p.ProductVariants)
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
                StockQuantity = p.StockQuantity,
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.ProductVariantValues)
                    .ThenInclude(pvv => pvv.ProductValue)
                        .ThenInclude(pv => pv!.ProductOption)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        await EnsureVariantsExistAsync(product);

        var refreshedProduct = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.ProductVariantValues)
                    .ThenInclude(pvv => pvv.ProductValue)
                        .ThenInclude(pv => pv!.ProductOption)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        return refreshedProduct is null
            ? NotFound(new { message = "Không tìm thấy sản phẩm." })
            : Ok(ToResponse(refreshedProduct));
    }

    [HttpGet("{id:guid}/variants")]
    public async Task<ActionResult<IEnumerable<ProductVariantResponse>>> GetVariants(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.ProductVariantValues)
                    .ThenInclude(pvv => pvv.ProductValue)
                        .ThenInclude(pv => pv!.ProductOption)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        var variants = ToResponse(product).ProductVariants;
        return Ok(variants);
    }

    [HttpPost("{id:guid}/generate-variants")]
    public async Task<ActionResult<object>> GenerateVariants(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.ProductVariantValues)
                    .ThenInclude(pvv => pvv.ProductValue)
                        .ThenInclude(pv => pv!.ProductOption)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        await EnsureVariantsExistAsync(product);

        var refreshedProduct = await _context.Products
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.ProductVariantValues)
                    .ThenInclude(pvv => pvv.ProductValue)
                        .ThenInclude(pv => pv!.ProductOption)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (refreshedProduct is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        return Ok(new
        {
            productId = refreshedProduct.ProductId,
            variants = ToResponse(refreshedProduct).ProductVariants
        });
    }

    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(Guid id, Guid variantId, ProductVariantUpdateRequest request)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(pv => pv.ProductVariantId == variantId && pv.ProductId == id);

        if (variant is null)
        {
            return NotFound(new { message = "Không tìm thấy biến thể sản phẩm." });
        }

        var normalizedSku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSku) &&
            await _context.ProductVariants.AnyAsync(pv => pv.ProductVariantId != variantId && pv.Sku == normalizedSku))
        {
            ModelState.AddModelError(nameof(request.Sku), "SKU đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        variant.Sku = normalizedSku;
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.Status = request.IsActive;
        variant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(pv => pv.ProductVariantId == variantId && pv.ProductId == id);

        if (variant is null)
        {
            return NotFound(new { message = "Không tìm thấy biến thể sản phẩm." });
        }

        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/variants/bulk/status")]
    public async Task<IActionResult> BulkUpdateVariantStatus(Guid id, ProductVariantBulkStatusRequest request)
    {
        if (request.VariantIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.VariantIds), "Vui lòng chọn ít nhất một biến thể.");
            return ValidationProblem(ModelState);
        }

        var variants = await _context.ProductVariants
            .Where(pv => pv.ProductId == id && request.VariantIds.Contains(pv.ProductVariantId))
            .ToListAsync();

        foreach (var variant in variants)
        {
            variant.Status = request.IsActive;
            variant.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { affected = variants.Count });
    }

    [HttpPost("{id:guid}/variants/bulk/delete")]
    public async Task<IActionResult> BulkDeleteVariants(Guid id, ProductVariantBulkDeleteRequest request)
    {
        if (request.VariantIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.VariantIds), "Vui lòng chọn ít nhất một biến thể.");
            return ValidationProblem(ModelState);
        }

        var variants = await _context.ProductVariants
            .Where(pv => pv.ProductId == id && request.VariantIds.Contains(pv.ProductVariantId))
            .ToListAsync();

        _context.ProductVariants.RemoveRange(variants);
        await _context.SaveChangesAsync();
        return Ok(new { affected = variants.Count });
    }

    [HttpPost("{id:guid}/variants/bulk/stock-adjust")]
    public async Task<IActionResult> BulkAdjustVariantStock(Guid id, ProductVariantBulkStockAdjustRequest request)
    {
        if (request.VariantIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.VariantIds), "Vui lòng chọn ít nhất một biến thể.");
            return ValidationProblem(ModelState);
        }

        var variants = await _context.ProductVariants
            .Where(pv => pv.ProductId == id && request.VariantIds.Contains(pv.ProductVariantId))
            .ToListAsync();

        foreach (var variant in variants)
        {
            var nextQty = variant.StockQuantity + request.DeltaQuantity;
            nextQty = Math.Max(0, Math.Min(short.MaxValue, nextQty));
            variant.StockQuantity = (short)nextQty;
            variant.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { affected = variants.Count });
    }

    private async Task EnsureVariantsExistAsync(Product product)
    {
        if (product.ProductOptions == null || !product.ProductOptions.Any()) return;

        var validOptions = product.ProductOptions
            .Where(o => o.ProductValues != null && o.ProductValues.Any())
            .Select(o => o.ProductValues.ToList())
            .ToList();

        if (!validOptions.Any()) return;

        var combinations = new List<List<ProductValue>>();
        void Generate(int depth, List<ProductValue> current)
        {
            if (depth == validOptions.Count)
            {
                combinations.Add(new List<ProductValue>(current));
                return;
            }
            foreach (var val in validOptions[depth])
            {
                current.Add(val);
                Generate(depth + 1, current);
                current.RemoveAt(current.Count - 1);
            }
        }

        Generate(0, new List<ProductValue>());

        bool added = false;
        foreach (var combo in combinations)
        {
            var comboIds = combo.Select(c => c.ProductValueId).OrderBy(id => id).ToList();

            bool exists = product.ProductVariants.Any(pv => 
                pv.ProductVariantValues != null &&
                pv.ProductVariantValues.Count == comboIds.Count &&
                pv.ProductVariantValues.Select(pvv => pvv.ProductValueId).OrderBy(id => id).SequenceEqual(comboIds)
            );

            if (!exists)
            {
                var variant = new ProductVariant
                {
                    ProductId = product.ProductId,
                    Price = combo.Sum(c => c.AdditionalPrice),
                    StockQuantity = product.StockQuantity > 0 ? product.StockQuantity : (short)0,
                    Status = true,
                    ProductVariantValues = combo.Select(c => new ProductVariantValue { ProductValueId = c.ProductValueId }).ToList()
                };
                _context.ProductVariants.Add(variant);
                product.ProductVariants.Add(variant);
                added = true;
            }
        }

        if (added)
        {
            await _context.SaveChangesAsync();
        }
    }

    [HttpGet("admin/products")]
    public async Task<ActionResult<IEnumerable<AdminProductResponse>>> GetAllForAdmin([FromQuery] Guid? categoryId = null)
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

    [HttpGet("admin/{id:guid}")]
    public async Task<ActionResult<AdminProductResponse>> GetByIdForAdmin(Guid id)
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
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Sku = request.Name.Trim().ToUpper().Substring(0, Math.Min(30, request.Name.Trim().Length)).Replace(" ", "-"),
            MinimumStock = request.MinimumStock,
            Status = request.Status?.ToLower() == "active",
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, ToResponse(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request)
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
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Status = request.Status?.ToLower() == "active";
        product.MinimumStock = request.MinimumStock;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
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
            StockQuantity = product.StockQuantity,
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
                .ToList(),
            ProductVariants = product.ProductVariants
                .Select(pv => new ProductVariantResponse
                {
                    ProductVariantId = pv.ProductVariantId,
                    ProductId = pv.ProductId,
                    Price = pv.Price,
                    StockQuantity = pv.StockQuantity,
                    Sku = pv.Sku,
                    IsActive = pv.Status,
                    VariantValues = pv.ProductVariantValues != null ? pv.ProductVariantValues
                        .Where(pvv => pvv.ProductValue != null && pvv.ProductValue.ProductOption != null)
                        .Select(pvv => new ProductVariantValueResponse
                        {
                            ProductOptionId = pvv.ProductValue!.ProductOptionId,
                            OptionName = pvv.ProductValue.ProductOption!.OptionName,
                            ProductValueId = pvv.ProductValueId,
                            ValueName = pvv.ProductValue.ValueName
                        }).ToList() : new List<ProductVariantValueResponse>()
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
            StockQuantity = product.StockQuantity,
            MinimumStock = product.MinimumStock,
            CategoryId = product.CategoryId,
            CategoryName = categoryName ?? product.Category?.CategoryName,
            Description = product.Description,
            Status = product.Status ? "active" : "inactive",
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
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
