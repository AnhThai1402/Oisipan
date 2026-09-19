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
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
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
                Sku = p.Sku,
                MinimumStock = p.MinimumStock,
                Status = p.Status ? "active" : "inactive"
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

        try
        {
            await EnsureVariantsExistAsync(product);
        }
        catch (DbUpdateException ex) when (IsCombinationKeyConflict(ex))
        {
            _context.ChangeTracker.Clear();
        }

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

        try
        {
            await EnsureVariantsExistAsync(product);
        }
        catch (DbUpdateException ex) when (IsCombinationKeyConflict(ex))
        {
            _context.ChangeTracker.Clear();
        }

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

    [HttpPost("{id:guid}/variants/add-batch")]
    public async Task<IActionResult> AddVariantBatch(Guid id, ProductVariantAddBatchRequest request)
    {
        var product = await _context.Products
            .Include(p => p.ProductOptions)
                .ThenInclude(o => o.ProductValues)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.ProductVariantValues)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        var attributes = (request.Attributes ?? new())
            .Where(a => !string.IsNullOrWhiteSpace(a.Name) && a.Values != null && a.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            .ToList();

        if (attributes.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Attributes), "Vui lòng nhập ít nhất một thuộc tính và giá trị.");
            return ValidationProblem(ModelState);
        }

        var valueGroups = new List<List<ProductValue>>();
        foreach (var attr in attributes)
        {
            var optionName = attr.Name.Trim();
            var option = product.ProductOptions.FirstOrDefault(o => o.OptionName.Equals(optionName, StringComparison.OrdinalIgnoreCase));
            if (option is null)
            {
                option = new ProductOption { ProductId = id, OptionName = optionName };
                _context.ProductOptions.Add(option);
                product.ProductOptions.Add(option);
            }

            var group = new List<ProductValue>();
            foreach (var raw in attr.Values.Select(v => v.Trim()).Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var value = option.ProductValues.FirstOrDefault(v => v.ValueName.Equals(raw, StringComparison.OrdinalIgnoreCase));
                if (value is null)
                {
                    value = new ProductValue { ProductOptionId = option.ProductOptionId, ProductOption = option, ValueName = raw, AdditionalPrice = 0 };
                    _context.ProductValues.Add(value);
                    option.ProductValues.Add(value);
                }
                group.Add(value);
            }

            if (group.Count > 0)
            {
                valueGroups.Add(group);
            }
        }

        if (valueGroups.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Attributes), "Vui lòng nhập ít nhất một thuộc tính và giá trị hợp lệ.");
            return ValidationProblem(ModelState);
        }

        await _context.SaveChangesAsync();

        var combinations = new List<List<ProductValue>> { new() };
        foreach (var group in valueGroups)
        {
            var next = new List<List<ProductValue>>();
            foreach (var combo in combinations)
            {
                foreach (var value in group)
                {
                    var copy = new List<ProductValue>(combo) { value };
                    next.Add(copy);
                }
            }
            combinations = next;
        }

        var existingKeys = new HashSet<string>(
            product.ProductVariants.Select(variant =>
                !string.IsNullOrWhiteSpace(variant.CombinationKey)
                    ? variant.CombinationKey!
                    : BuildCombinationKey(variant.ProductVariantValues.Select(item => item.ProductValueId))),
            StringComparer.Ordinal);

        var created = 0;
        foreach (var combo in combinations)
        {
            var combinationKey = BuildCombinationKey(combo.Select(c => c.ProductValueId));
            if (existingKeys.Contains(combinationKey))
            {
                continue;
            }

            var variant = new ProductVariant
            {
                ProductId = id,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                Status = true,
                CombinationKey = combinationKey,
                Sku = await GenerateUniqueSkuAsync(product),
                ProductVariantValues = combo.Select(c => new ProductVariantValue { ProductValueId = c.ProductValueId }).ToList()
            };
            _context.ProductVariants.Add(variant);
            existingKeys.Add(combinationKey);
            created++;
        }

        await _context.SaveChangesAsync();
        return Ok(new { created });
    }

    [HttpPut("{id:guid}/variants/{variantId:guid}/full")]
    public async Task<IActionResult> UpdateVariantFull(Guid id, Guid variantId, ProductVariantFullUpdateRequest request)
    {
        var product = await _context.Products
            .Include(p => p.ProductOptions)
                .ThenInclude(o => o.ProductValues)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.ProductVariantValues)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        var variant = product.ProductVariants.FirstOrDefault(v => v.ProductVariantId == variantId);
        if (variant is null)
        {
            return NotFound(new { message = "Không tìm thấy biến thể sản phẩm." });
        }

        var attributes = (request.Attributes ?? new())
            .Where(a => !string.IsNullOrWhiteSpace(a.Name) && a.Values != null && a.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            .ToList();

        if (attributes.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Attributes), "Vui lòng nhập ít nhất một thuộc tính và giá trị.");
            return ValidationProblem(ModelState);
        }

        var newValues = new List<ProductValue>();
        foreach (var attr in attributes)
        {
            var optionName = attr.Name.Trim();
            var option = product.ProductOptions.FirstOrDefault(o => o.OptionName.Equals(optionName, StringComparison.OrdinalIgnoreCase));
            if (option is null)
            {
                option = new ProductOption { ProductId = id, OptionName = optionName };
                _context.ProductOptions.Add(option);
                product.ProductOptions.Add(option);
            }

            var valueName = attr.Values.Select(v => v.Trim()).FirstOrDefault(v => v.Length > 0);
            if (valueName is null) continue;

            var value = option.ProductValues.FirstOrDefault(v => v.ValueName.Equals(valueName, StringComparison.OrdinalIgnoreCase));
            if (value is null)
            {
                value = new ProductValue { ProductOptionId = option.ProductOptionId, ProductOption = option, ValueName = valueName, AdditionalPrice = 0 };
                _context.ProductValues.Add(value);
                option.ProductValues.Add(value);
            }
            newValues.Add(value);
        }

        if (newValues.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Attributes), "Vui lòng nhập ít nhất một thuộc tính và giá trị hợp lệ.");
            return ValidationProblem(ModelState);
        }

        await _context.SaveChangesAsync();

        var newKey = BuildCombinationKey(newValues.Select(v => v.ProductValueId));
        var conflict = product.ProductVariants.Any(v => v.ProductVariantId != variantId &&
            (!string.IsNullOrWhiteSpace(v.CombinationKey)
                ? v.CombinationKey
                : BuildCombinationKey(v.ProductVariantValues.Select(x => x.ProductValueId))) == newKey);

        if (conflict)
        {
            ModelState.AddModelError(nameof(request.Attributes), "Đã tồn tại biến thể khác với tổ hợp thuộc tính này.");
            return ValidationProblem(ModelState);
        }

        _context.ProductVariantValues.RemoveRange(variant.ProductVariantValues);
        variant.ProductVariantValues = newValues
            .Select(v => new ProductVariantValue { ProductVariantId = variantId, ProductValueId = v.ProductValueId })
            .ToList();
        variant.CombinationKey = newKey;
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string> GenerateUniqueSkuAsync(Product product)
    {
        var prefix = string.IsNullOrWhiteSpace(product.Sku) ? "PRD" : product.Sku;
        string sku;
        do
        {
            sku = $"{prefix}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }
        while (await _context.ProductVariants.AnyAsync(v => v.Sku == sku));

        return sku;
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

        var existingKeys = new HashSet<string>(
            product.ProductVariants.Select(variant =>
                !string.IsNullOrWhiteSpace(variant.CombinationKey)
                    ? variant.CombinationKey!
                    : BuildCombinationKey(variant.ProductVariantValues.Select(item => item.ProductValueId))),
            StringComparer.Ordinal);

        bool added = false;
        foreach (var combo in combinations)
        {
            var combinationKey = BuildCombinationKey(combo.Select(c => c.ProductValueId));
            if (existingKeys.Contains(combinationKey))
            {
                continue;
            }

            var variant = new ProductVariant
            {
                ProductId = product.ProductId,
                Price = combo.Sum(c => c.AdditionalPrice),
                StockQuantity = product.StockQuantity > 0 ? product.StockQuantity : (short)0,
                Status = true,
                CombinationKey = combinationKey,
                ProductVariantValues = combo.Select(c => new ProductVariantValue { ProductValueId = c.ProductValueId }).ToList()
            };
            _context.ProductVariants.Add(variant);
            existingKeys.Add(combinationKey);
            added = true;
        }

        if (added)
        {
            await _context.SaveChangesAsync();
        }
    }

    private static string BuildCombinationKey(IEnumerable<Guid>? productValueIds)
    {
        if (productValueIds == null)
        {
            return string.Empty;
        }

        return string.Join("|", productValueIds
            .Distinct()
            .OrderBy(id => id)
            .Select(id => id.ToString("N")));
    }

    private static bool IsCombinationKeyConflict(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_ProductVariants_ProductId_CombinationKey", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet("admin/products")]
    public async Task<ActionResult<IEnumerable<AdminProductResponse>>> GetAllForAdmin([FromQuery] Guid? categoryId = null)
    {
        var query = _context.Products.AsQueryable();

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
        
        var defaultVariant = new ProductVariant
        {
            Product = product,
            Price = 0,
            StockQuantity = request.StockQuantity > 0 ? request.StockQuantity : (short)0,
            Status = true
        };
        _context.ProductVariants.Add(defaultVariant);

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
        product.UpdatedAt = DateTime.UtcNow;

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
            Sku = product.Sku,
            MinimumStock = product.MinimumStock,
            Status = product.Status ? "active" : "inactive"
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
            Sku = product.Sku,
            Status = product.Status ? "active" : "inactive",
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
