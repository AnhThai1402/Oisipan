using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "Cart";
    private readonly IHttpClientFactory _httpClientFactory;

    public CartController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? returnUrl = null)
    {
        var product = await Api.GetFromJsonAsyncWithOptions<ProductCatalogViewModel>($"api/products/{productId}");
        if (product is null || product.Quantity <= 0)
        {
            TempData["CartError"] = "Sản phẩm không tồn tại hoặc đã hết hàng.";
            return RedirectBack(returnUrl);
        }

        var cart = GetCart();
        var existing = cart.Items.FirstOrDefault(item => item.ProductId == productId);

        if (existing is null)
        {
            cart.Items.Add(new CartItemViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                UnitPrice = product.Price,
                Image = product.Image,
                Quantity = 1
            });
        }
        else if (existing.Quantity < product.Quantity)
        {
            existing.Quantity++;
        }
        else
        {
            TempData["CartError"] = $"{product.Name} chỉ còn {product.Quantity} sản phẩm.";
            return RedirectBack(returnUrl);
        }

        SaveCart(cart);
        TempData["CartMessage"] = $"Đã thêm {product.Name} vào giỏ hàng.";
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity, string? returnUrl = null)
    {
        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Sản phẩm không tìm thấy trong giỏ." });
            }
            return RedirectBack(returnUrl);
        }

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var product = await Api.GetFromJsonAsyncWithOptions<ProductCatalogViewModel>($"api/products/{productId}");
            if (product is null || product.Quantity <= 0)
            {
                cart.Items.Remove(item);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Sản phẩm đã hết hàng." });
                }
                TempData["CartError"] = "Sản phẩm đã hết hàng và được xóa khỏi giỏ.";
            }
            else
            {
                item.Quantity = Math.Min(quantity, product.Quantity);
                if (quantity > product.Quantity)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new 
                        { 
                            success = true, 
                            message = $"{product.Name} chỉ còn {product.Quantity} sản phẩm.", 
                            cartTotal = cart.Items.Sum(x => x.LineTotal),
                            items = cart.Items.Select(x => new { x.ProductId, x.Quantity, lineTotal = x.LineTotal }).ToList()
                        });
                    }
                    TempData["CartError"] = $"{product.Name} chỉ còn {product.Quantity} sản phẩm.";
                }
            }
        }

        SaveCart(cart);
        
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new 
            { 
                success = true, 
                cartTotal = cart.Items.Sum(x => x.LineTotal),
                items = cart.Items.Select(x => new { x.ProductId, x.Quantity, lineTotal = x.LineTotal }).ToList()
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId, string? returnUrl = null)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(item => item.ProductId == productId);
        SaveCart(cart);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new 
            { 
                success = true, 
                cartTotal = cart.Items.Sum(x => x.LineTotal),
                items = cart.Items.Select(x => new { x.ProductId, x.Quantity, lineTotal = x.LineTotal }).ToList()
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, string? returnUrl = null)
    {
        var cart = GetCart();
        if (!cart.Items.Any())
        {
            TempData["CartError"] = "Giỏ hàng đang trống.";
            return RedirectBack(returnUrl);
        }

        if (!ModelState.IsValid)
        {
            TempData["CartError"] = "Vui lòng nhập đầy đủ thông tin giao hàng.";
            return RedirectBack(returnUrl);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var request = new ApiOrderCreateRequest
        {
            UserId = userId,
            PaymentMethod = model.PaymentMethod,
            ShippingAddress = model.CustomerAddress,
            Items = cart.Items.Select(item => new ApiOrderItemRequest
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };

        var response = await Api.PostAsJsonAsync("api/orders", request);
        if (!response.IsSuccessStatusCode)
        {
            TempData["CartError"] = await ReadApiMessage(response);
            return RedirectBack(returnUrl);
        }

        HttpContext.Session.Remove(CartSessionKey);
        TempData["CartMessage"] = "Đặt hàng thành công. Đơn hàng của bạn đã được ghi nhận.";
        return RedirectToAction("Index", "Orders");
    }

    private CartViewModel GetCart()
    {
        var items = HttpContext.Session.GetJson<List<CartItemViewModel>>(CartSessionKey) ?? new();
        return new CartViewModel { Items = items };
    }

    private void SaveCart(CartViewModel cart)
    {
        HttpContext.Session.SetJson(CartSessionKey, cart.Items);
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private async Task<string> ReadApiMessage(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "Bạn cần đăng nhập để đặt hàng.";
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Không thể tạo đơn hàng. Vui lòng thử lại.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "Không thể tạo đơn hàng. Vui lòng thử lại.";
            }
        }
        catch (JsonException)
        {
        }

        return "Không thể tạo đơn hàng. Vui lòng thử lại.";
    }

    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}
