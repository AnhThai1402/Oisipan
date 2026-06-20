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
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .Include(p => p.ProductVariants)
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
                    .ToList(),
                ProductVariants = p.ProductVariants
                    .OrderBy(pv => pv.Size)
                    .ThenBy(pv => pv.Filling)
                    .Select(pv => new ProductVariantResponse
                    {
                        ProductVariantId = pv.ProductVariantId,
                        ProductId = pv.ProductId,
                        Size = pv.Size,
                        Filling = pv.Filling,
                        AdditionalPrice = pv.AdditionalPrice,
                        Quantity = pv.Quantity,
                        IsActive = pv.IsActive
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
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        return product is null ? NotFound(new { message = "Không tìm thấy sản phẩm." }) : Ok(ToResponse(product));
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

        ValidateProductVariants(request.ProductVariants);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Image = string.IsNullOrWhiteSpace(request.Image) ? null : request.Image.Trim(),
            Quantity = request.ProductVariants.Sum(variant => variant.Quantity),
            CategoryId = request.CategoryId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ProductVariants = BuildProductVariants(request.ProductVariants)
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, ToResponse(product));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.ProductOptions)
                .ThenInclude(po => po.ProductValues)
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.ProductId == id);
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

        ValidateProductVariants(request.ProductVariants);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        product.Name = request.Name.Trim();
        product.Price = request.Price;
        product.Image = string.IsNullOrWhiteSpace(request.Image) ? null : request.Image.Trim();
        product.Quantity = request.ProductVariants.Sum(variant => variant.Quantity);
        product.CategoryId = request.CategoryId;
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        _context.ProductVariants.RemoveRange(product.ProductVariants);
        await _context.SaveChangesAsync();

        product.ProductVariants = BuildProductVariants(request.ProductVariants);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

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
                .ToList(),
            ProductVariants = product.ProductVariants
                .OrderBy(pv => pv.Size)
                .ThenBy(pv => pv.Filling)
                .Select(pv => new ProductVariantResponse
                {
                    ProductVariantId = pv.ProductVariantId,
                    ProductId = pv.ProductId,
                    Size = pv.Size,
                    Filling = pv.Filling,
                    AdditionalPrice = pv.AdditionalPrice,
                    Quantity = pv.Quantity,
                    IsActive = pv.IsActive
                })
                .ToList()
        };
    }

    private void ValidateProductVariants(List<ProductVariantRequest> variants)
    {
        if (variants.Count == 0)
        {
            ModelState.AddModelError(
                nameof(ProductRequest.ProductVariants),
                "Vui long them it nhat mot bien the san pham.");
            return;
        }

        if (variants.Any(variant =>
            string.IsNullOrWhiteSpace(variant.Size) ||
            string.IsNullOrWhiteSpace(variant.Filling)))
        {
            ModelState.AddModelError(
                nameof(ProductRequest.ProductVariants),
                "Size va nhan banh khong duoc de trong.");
        }

        var hasDuplicates = variants
            .GroupBy(
                variant => $"{variant.Size.Trim()}|{variant.Filling.Trim()}",
                StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (hasDuplicates)
        {
            ModelState.AddModelError(
                nameof(ProductRequest.ProductVariants),
                "To hop size va nhan banh khong duoc trung nhau.");
        }
    }

    private static List<ProductVariant> BuildProductVariants(IEnumerable<ProductVariantRequest> variants)
    {
        return variants.Select(variant => new ProductVariant
        {
            Size = variant.Size.Trim(),
            Filling = variant.Filling.Trim(),
            AdditionalPrice = variant.AdditionalPrice,
            Quantity = variant.Quantity,
            IsActive = variant.IsActive
        }).ToList();
    }

    private void ValidateProductOptions(List<ProductOptionInputRequest> options)
    {
        foreach (var requiredOption in new[] { "size", "nhan banh" })
        {
            var option = options.FirstOrDefault(item => NormalizeOptionName(item.OptionName) == requiredOption);
            if (option is null || option.ProductValues.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(ProductRequest.ProductOptions),
                    $"Vui long them it nhat mot gia tri cho {requiredOption}.");
            }
        }

        if (options.GroupBy(option => NormalizeOptionName(option.OptionName)).Any(group => group.Count() > 1))
        {
            ModelState.AddModelError(nameof(ProductRequest.ProductOptions), "Ten tuy chon khong duoc trung nhau.");
        }

        foreach (var option in options)
        {
            if (option.ProductValues
                .GroupBy(value => value.ValueName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1))
            {
                ModelState.AddModelError(
                    nameof(ProductRequest.ProductOptions),
                    $"Gia tri trong tuy chon {option.OptionName} khong duoc trung nhau.");
            }
        }
    }

    private static List<ProductOption> BuildProductOptions(IEnumerable<ProductOptionInputRequest> options)
    {
        return options.Select(option => new ProductOption
        {
            OptionName = NormalizeOptionName(option.OptionName) == "nhan banh"
                ? "Nhân bánh"
                : option.OptionName.Trim(),
            ProductValues = option.ProductValues.Select(value => new ProductValue
            {
                ValueName = value.ValueName.Trim(),
                AdditionalPrice = value.AdditionalPrice
            }).ToList()
        }).ToList();
    }

    private static string NormalizeOptionName(string optionName)
    {
        return optionName.Trim().ToLowerInvariant()
            .Replace("â", "a")
            .Replace("ă", "a");
    }
}
