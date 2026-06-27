using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : AdminBaseController
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var products = await Api.GetFromJsonAsyncWithOptions<List<ProductAdminViewModel>>("api/products/admin/products") ?? new();
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
        if (!ModelState.IsValid)
        {
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        // If an image file was uploaded, send it first to the API upload endpoint
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await model.ImageFile.CopyToAsync(ms);
            ms.Position = 0;

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(ms);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ImageFile.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "file", model.ImageFile.FileName);

            try
            {
                var uploadResp = await Api.PostAsync("api/upload/image", content);
                if (!uploadResp.IsSuccessStatusCode)
                {
                    var contentText = await uploadResp.Content.ReadAsStringAsync();
                    string err = "Không thể tải ảnh lên. Vui lòng thử lại.";
                    try
                    {
                        using var docErr = System.Text.Json.JsonDocument.Parse(contentText);
                        if (docErr.RootElement.TryGetProperty("message", out var m)) err = m.GetString() ?? err;
                    }
                    catch { err = string.IsNullOrWhiteSpace(contentText) ? err : contentText; }

                    ModelState.AddModelError("ImageFile", err);
                    await PopulateCategories(model);
                    return View("CreateEdit", model);
                }

                // Parse returned URL
                var uploadJson = await uploadResp.Content.ReadAsStringAsync();
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(uploadJson);
                    if (doc.RootElement.TryGetProperty("url", out var u) || doc.RootElement.TryGetProperty("Url", out u))
                    {
                        model.Image = u.GetString();
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ImageFile", "Lỗi khi tải ảnh: " + ex.Message);
                await PopulateCategories(model);
                return View("CreateEdit", model);
            }
        }

        var payload = new
        {
            Name = model.Name,
            Price = model.Price,
            Image = model.Image,
            Quantity = model.Quantity,
            CategoryId = model.CategoryId,
            Description = model.Description,
            MinimumStock = model.MinimumStock,
            Status = model.Status
        };

        var response = await Api.PostAsJsonAsync("api/products", payload);
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
    public async Task<IActionResult> Edit(int id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        if (model is null) return NotFound();

        await PopulateCategories(model);
        return View("CreateEdit", model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        // Handle image file upload if present
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await model.ImageFile.CopyToAsync(ms);
            ms.Position = 0;

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(ms);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ImageFile.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "file", model.ImageFile.FileName);

            try
            {
                var uploadResp = await Api.PostAsync("api/upload/image", content);
                if (!uploadResp.IsSuccessStatusCode)
                {
                    var contentText = await uploadResp.Content.ReadAsStringAsync();
                    string err = "Không thể tải ảnh lên. Vui lòng thử lại.";
                    try
                    {
                        using var docErr = System.Text.Json.JsonDocument.Parse(contentText);
                        if (docErr.RootElement.TryGetProperty("message", out var m)) err = m.GetString() ?? err;
                    }
                    catch { err = string.IsNullOrWhiteSpace(contentText) ? err : contentText; }

                    ModelState.AddModelError("ImageFile", err);
                    await PopulateCategories(model);
                    return View("CreateEdit", model);
                }

                var uploadJson = await uploadResp.Content.ReadAsStringAsync();
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(uploadJson);
                    if (doc.RootElement.TryGetProperty("url", out var u) || doc.RootElement.TryGetProperty("Url", out u))
                    {
                        model.Image = u.GetString();
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ImageFile", "Lỗi khi tải ảnh: " + ex.Message);
                await PopulateCategories(model);
                return View("CreateEdit", model);
            }
        }

        var payload = new
        {
            Name = model.Name,
            Price = model.Price,
            Image = model.Image,
            Quantity = model.Quantity,
            CategoryId = model.CategoryId,
            Description = model.Description,
            MinimumStock = model.MinimumStock,
            Status = model.Status
        };

        var response = await Api.PutAsJsonAsync($"api/products/{id}", payload);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Không thể cập nhật sản phẩm.");
            await PopulateCategories(model);
            return View("CreateEdit", model);
        }

        SetFlashMessage("Cập nhật sản phẩm thành công", "edit");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var model = await Api.GetFromJsonAsyncWithOptions<ProductAdminViewModel>($"api/products/admin/{id}");
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteAsync($"api/products/{id}");
        SetFlashMessage(
            response.IsSuccessStatusCode ? "Xóa sản phẩm thành công" : "Không thể xóa sản phẩm.",
            response.IsSuccessStatusCode ? "delete" : "error");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int id, int quantity)
    {
        // Get current product
        var product = await Api.GetFromJsonAsync<ProductAdminViewModel>($"api/products/admin/{id}");
        if (product is null)
        {
            return NotFound();
        }

        // Update quantity
        product.Quantity = quantity;
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

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
