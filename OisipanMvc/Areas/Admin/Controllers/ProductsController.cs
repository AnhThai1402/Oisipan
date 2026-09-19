using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using FrontendMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : AdminBaseController
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IImageStorageService _imageStorageService;

    public ProductsController(
        IHttpClientFactory httpClientFactory,
        IImageStorageService imageStorageService)
    {
        _httpClientFactory = httpClientFactory;
        _imageStorageService = imageStorageService;
    }

    public async Task<IActionResult> Index([FromQuery] Guid? categoryId = null)
    {
        var url = categoryId.HasValue ? $"api/products/admin/products?categoryId={categoryId.Value}" : "api/products/admin/products";
        var products = await Api.GetFromJsonAsyncWithOptions<List<ProductAdminViewModel>>(url) ?? new();
        
        var categories = await Api.GetFromJsonAsyncWithOptions<List<CategoryAdminViewModel>>("api/categories") ?? new();
        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;
        
        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        var model = new ProductAdminViewModel();
        await PopulateCategories(model);
        return View("CreateEdit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductAdminViewModel model)
    {
        await SaveImageIfValid(model);
        if (!ModelState.IsValid)
        {
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        var response = await Api.PostAsJsonAsync("api/products", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            // Try to parse error details from API response
            try
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(errorContent))
                {
                    var errorJson = System.Text.Json.JsonDocument.Parse(errorContent);
                    var root = errorJson.RootElement;
                    
                    // Check for validation errors in ProblemDetails format
                    if (root.TryGetProperty("errors", out var errorsElement))
                    {
                        foreach (var property in errorsElement.EnumerateObject())
                        {
                            var fieldName = property.Name;
                            if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var error in property.Value.EnumerateArray())
                                {
                                    ModelState.AddModelError(fieldName, error.GetString() ?? "Lỗi không xác định");
                                }
                            }
                        }
                    }
                    else if (root.TryGetProperty("message", out var messageElement))
                    {
                        ModelState.AddModelError(string.Empty, messageElement.GetString() ?? "Không thể tạo sản phẩm");
                    }
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo sản phẩm. Vui lòng kiểm tra dữ liệu.");
            }

            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        SetFlashMessage("Tạo sản phẩm thành công", "create");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        if (model is null) return NotFound();

        await PopulateCategories(model);
        return View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductAdminViewModel model)
    {
        if (id != model.ProductId)
        {
            return BadRequest();
        }

        await SaveImageIfValid(model);
        if (!ModelState.IsValid)
        {
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        var response = await Api.PutAsJsonAsync($"api/products/{id}", ToRequest(model));
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Không thể cập nhật sản phẩm.");
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        SetFlashMessage("Cập nhật sản phẩm thành công", "edit");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpGet("/Admin/Products/{id:guid}/Variants")]
    public async Task<IActionResult> Variants(Guid id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet("/Admin/Products/{id:guid}/Variants/Data")]
    public async Task<IActionResult> GetVariantsData(Guid id)
    {
        var product = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        if (product is null)
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm." });
        }

        var options = await Api.GetFromJsonAsyncWithOptions<List<ProductOptionAdminViewModel>>($"api/productoptions/product/{id}") ?? new();
        var variants = await Api.GetFromJsonAsyncWithOptions<List<ProductVariantAdminViewModel>>($"api/products/{id}/variants") ?? new();

        return Ok(new
        {
            productId = product.ProductId,
            productName = product.ProductName,
            options,
            variants
        });
    }

    [HttpPost("/Admin/Products/{id:guid}/Variants/Options")]
    public async Task<IActionResult> CreateVariantOption(Guid id, [FromBody] VariantOptionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PostAsJsonAsync("api/productoptions", new
        {
            ProductId = id,
            request.OptionName
        });

        return await ToProxyResult(response);
    }

    [HttpPut("/Admin/Products/{id:guid}/Variants/Options/{optionId:guid}")]
    public async Task<IActionResult> UpdateVariantOption(Guid id, Guid optionId, [FromBody] VariantOptionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PutAsJsonAsync($"api/productoptions/{optionId}", new
        {
            ProductId = id,
            request.OptionName
        });

        return await ToProxyResult(response);
    }

    [HttpDelete("/Admin/Products/{id:guid}/Variants/Options/{optionId:guid}")]
    public async Task<IActionResult> DeleteVariantOption(Guid id, Guid optionId)
    {
        var response = await Api.DeleteAsync($"api/productoptions/{optionId}");
        return await ToProxyResult(response);
    }

    [HttpPost("/Admin/Products/{id:guid}/Variants/Options/{optionId:guid}/Values")]
    public async Task<IActionResult> CreateVariantValue(Guid id, Guid optionId, [FromBody] VariantValueRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PostAsJsonAsync("api/productvalues", new
        {
            ProductOptionId = optionId,
            request.ValueName,
            request.AdditionalPrice
        });

        return await ToProxyResult(response);
    }

    [HttpPut("/Admin/Products/{id:guid}/Variants/Values/{valueId:guid}")]
    public async Task<IActionResult> UpdateVariantValue(Guid id, Guid valueId, [FromBody] VariantValueUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PutAsJsonAsync($"api/productvalues/{valueId}", new
        {
            request.ProductOptionId,
            request.ValueName,
            request.AdditionalPrice
        });

        return await ToProxyResult(response);
    }

    [HttpDelete("/Admin/Products/{id:guid}/Variants/Values/{valueId:guid}")]
    public async Task<IActionResult> DeleteVariantValue(Guid id, Guid valueId)
    {
        var response = await Api.DeleteAsync($"api/productvalues/{valueId}");
        return await ToProxyResult(response);
    }

    [HttpPost("/Admin/Products/{id:guid}/Variants/Generate")]
    public async Task<IActionResult> GenerateVariantsManual(Guid id)
    {
        var response = await Api.PostAsync($"api/products/{id}/generate-variants", null);
        return await ToProxyResult(response);
    }

    [HttpGet("/Admin/Products/{id:guid}/Variants/Add")]
    public async Task<IActionResult> AddVariant(Guid id)
    {
        var product = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        if (product is null)
        {
            return NotFound();
        }

        var model = new VariantFormViewModel
        {
            ProductId = id,
            ProductName = product.Name,
            Mode = "add",
            Attributes = new List<VariantFormAttribute> { new() }
        };

        return View("VariantForm", model);
    }

    [HttpGet("/Admin/Products/{id:guid}/Variants/{variantId:guid}/Edit")]
    public async Task<IActionResult> EditVariant(Guid id, Guid variantId)
    {
        var product = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        if (product is null)
        {
            return NotFound();
        }

        var variants = await Api.GetFromJsonAsyncWithOptions<List<ProductVariantAdminViewModel>>($"api/products/{id}/variants") ?? new();
        var variant = variants.FirstOrDefault(v => v.ProductVariantId == variantId);
        if (variant is null)
        {
            return NotFound();
        }

        var model = new VariantFormViewModel
        {
            ProductId = id,
            ProductName = product.Name,
            Mode = "edit",
            VariantId = variantId,
            Price = variant.Price,
            StockQuantity = variant.StockQuantity,
            Attributes = variant.VariantValues.Count > 0
                ? variant.VariantValues.Select(v => new VariantFormAttribute { Name = v.OptionName, Values = v.ValueName }).ToList()
                : new List<VariantFormAttribute> { new() }
        };

        return View("VariantForm", model);
    }

    [HttpPost("/Admin/Products/{id:guid}/Variants/AddBatch")]
    public async Task<IActionResult> AddVariantBatch(Guid id, [FromBody] VariantAddBatchRequest request)
    {
        var response = await Api.PostAsJsonAsync($"api/products/{id}/variants/add-batch", request);
        return await ToProxyResult(response);
    }

    [HttpPut("/Admin/Products/{id:guid}/Variants/{variantId:guid}/Full")]
    public async Task<IActionResult> UpdateVariantFull(Guid id, Guid variantId, [FromBody] VariantFullUpdateRequest request)
    {
        var response = await Api.PutAsJsonAsync($"api/products/{id}/variants/{variantId}/full", request);
        return await ToProxyResult(response);
    }

    [HttpPut("/Admin/Products/{id:guid}/Variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(Guid id, Guid variantId, [FromBody] VariantUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PutAsJsonAsync($"api/products/{id}/variants/{variantId}", request);
        return await ToProxyResult(response);
    }

    [HttpDelete("/Admin/Products/{id:guid}/Variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId)
    {
        var response = await Api.DeleteAsync($"api/products/{id}/variants/{variantId}");
        return await ToProxyResult(response);
    }

    [HttpPost("/Admin/Products/{id:guid}/Variants/Bulk/Status")]
    public async Task<IActionResult> BulkUpdateVariantStatus(Guid id, [FromBody] VariantBulkStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PostAsJsonAsync($"api/products/{id}/variants/bulk/status", request);
        return await ToProxyResult(response);
    }

    [HttpPost("/Admin/Products/{id:guid}/Variants/Bulk/Delete")]
    public async Task<IActionResult> BulkDeleteVariants(Guid id, [FromBody] VariantBulkDeleteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PostAsJsonAsync($"api/products/{id}/variants/bulk/delete", request);
        return await ToProxyResult(response);
    }

    [HttpPost("/Admin/Products/{id:guid}/Variants/Bulk/StockAdjust")]
    public async Task<IActionResult> BulkAdjustVariantStock(Guid id, [FromBody] VariantBulkStockAdjustRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await Api.PostAsJsonAsync($"api/products/{id}/variants/bulk/stock-adjust", request);
        return await ToProxyResult(response);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await Api.DeleteAsync($"api/products/{id}");
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Xóa sản phẩm thành công" : "Không thể xóa sản phẩm.",
            response.IsSuccessStatusCode ? "delete" : "error");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(Guid id, int quantity)
    {
        // Get current product
        var product = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        if (product is null)
        {
            return NotFound();
        }

        // Update quantity
        product.StockQuantity = (short)quantity;
        var response = await Api.PutAsJsonAsync($"api/products/{id}", product);

        SetFlashMessage(
            response.IsSuccessStatusCode ? "Cập nhật tồn kho thành công." : "Không thể cập nhật. Vui lòng thử lại.",
            response.IsSuccessStatusCode ? "edit" : "error");

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategories(ProductAdminViewModel model)
    {
        var categories = await Api.GetFromJsonAsyncWithOptions<List<CategoryAdminViewModel>>("api/categories") ?? new();
        model.Categories = categories.Select(category => new SelectListItem
        {
            Value = category.CategoryId.ToString(),
            Text = category.CategoryName,
            Selected = category.CategoryId == model.CategoryId
        }).ToList();
    }

    private static object ToRequest(ProductAdminViewModel model) => new
    {
        model.Name,
        model.Price,
        model.Image,
        model.StockQuantity,
        model.CategoryId,
        model.Description
    };

    private async Task SaveImageIfValid(ProductAdminViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0)
        {
            return;
        }

        if (!AllowedImageTypes.Contains(model.ImageFile.ContentType))
        {
            ModelState.AddModelError(
                nameof(model.ImageFile),
                "Ảnh sản phẩm phải là JPG, PNG, WEBP hoặc GIF.");
            return;
        }

        if (model.ImageFile.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(
                nameof(model.ImageFile),
                "Dung lượng ảnh không được vượt quá 5MB.");
            return;
        }

        try
        {
            model.Image = await _imageStorageService.UploadProductImageAsync(
                model.ImageFile,
                HttpContext.RequestAborted);
            ModelState.Remove(nameof(model.Image));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            ModelState.AddModelError(nameof(model.ImageFile), $"Không thể tải ảnh: {ex.Message}");
        }
    }

    private static async Task<IActionResult> ToProxyResult(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new StatusCodeResult((int)response.StatusCode);
        }

        return new ContentResult
        {
            Content = payload,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}

public class VariantOptionRequest
{
    [Required]
    [StringLength(100)]
    public string OptionName { get; set; } = string.Empty;
}

public class VariantValueRequest
{
    [Required]
    [StringLength(100)]
    public string ValueName { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal AdditionalPrice { get; set; }
}

public class VariantValueUpdateRequest : VariantValueRequest
{
    [Required]
    public Guid ProductOptionId { get; set; }
}

public class VariantUpdateRequest
{
    [StringLength(50)]
    public string? Sku { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, short.MaxValue)]
    public short StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}

public class VariantBulkStatusRequest
{
    [Required]
    public List<Guid> VariantIds { get; set; } = new();

    public bool IsActive { get; set; }
}

public class VariantBulkDeleteRequest
{
    [Required]
    public List<Guid> VariantIds { get; set; } = new();
}

public class VariantBulkStockAdjustRequest
{
    [Required]
    public List<Guid> VariantIds { get; set; } = new();

    [Range(-100000, 100000)]
    public int DeltaQuantity { get; set; }
}

public class VariantAttributeInput
{
    public string Name { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}

public class VariantAddBatchRequest
{
    [Required]
    public List<VariantAttributeInput> Attributes { get; set; } = new();

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, short.MaxValue)]
    public short StockQuantity { get; set; }
}

public class VariantFullUpdateRequest
{
    [Required]
    public List<VariantAttributeInput> Attributes { get; set; } = new();

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, short.MaxValue)]
    public short StockQuantity { get; set; }
}
