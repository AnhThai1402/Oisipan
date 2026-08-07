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
    public async Task<IActionResult> Add(Guid productId, Guid productVariantId, string? returnUrl = null)
    {
        var product = await Api.GetFromJsonAsyncWithOptions<ProductCatalogViewModel>($"api/products/{productId}");
        var variant = product?.ProductVariants.FirstOrDefault(v => v.ProductVariantId == productVariantId);
        
        if (product is null || variant is null || variant.StockQuantity <= 0)
        {
            TempData["CartError"] = "Biến thể sản phẩm không tồn tại hoặc đã hết hàng.";
            return RedirectBack(returnUrl);
        }

        var cart = GetCart();
        var existing = cart.Items.FirstOrDefault(item => item.ProductVariantId == productVariantId);

        if (existing is null)
        {
            cart.Items.Add(new CartItemViewModel
            {
                ProductVariantId = variant.ProductVariantId,
                ProductId = product.ProductId,
                Name = product.Name,
                VariantName = string.Join(" - ", variant.VariantValues.Select(vv => $"{vv.OptionName}: {vv.ValueName}")),
                UnitPrice = product.Price + variant.Price,
                Image = product.Image,
                Quantity = (byte)1
            });
        }
        else if (existing.Quantity < variant.StockQuantity)
        {
            existing.Quantity++;
        }
        else
        {
            TempData["CartError"] = $"{product.Name} chỉ còn {product.StockQuantity} sản phẩm.";
            return RedirectBack(returnUrl);
        }

        SaveCart(cart);
        TempData["CartMessage"] = $"Đã thêm {product.Name} vào giỏ hàng.";
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid productVariantId, int quantity, string? returnUrl = null)
    {
        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.ProductVariantId == productVariantId);
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
            var product = await Api.GetFromJsonAsyncWithOptions<ProductCatalogViewModel>($"api/products/{item.ProductId}");
            var variant = product?.ProductVariants.FirstOrDefault(v => v.ProductVariantId == productVariantId);
            
            if (product is null || variant is null || variant.StockQuantity <= 0)
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
                item.Quantity = (byte)Math.Min(quantity, variant.StockQuantity);
                if (quantity > variant.StockQuantity)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new 
                        { 
                            success = true, 
                            message = $"{product.Name} chỉ còn {variant.StockQuantity} sản phẩm.",
                            cartTotal = cart.Items.Sum(x => x.LineTotal),
                            items = cart.Items.Select(x => new { x.ProductVariantId, x.Quantity, lineTotal = x.LineTotal }).ToList()
                        });
                    }
                    TempData["CartError"] = $"{product.Name} chỉ còn {variant.StockQuantity} sản phẩm.";
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
                items = cart.Items.Select(x => new { x.ProductVariantId, x.Quantity, lineTotal = x.LineTotal }).ToList()
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(Guid productVariantId, string? returnUrl = null)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(item => item.ProductVariantId == productVariantId);
        SaveCart(cart);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new 
            { 
                success = true, 
                cartTotal = cart.Items.Sum(x => x.LineTotal),
                items = cart.Items.Select(x => new { x.ProductVariantId, x.Quantity, lineTotal = x.LineTotal }).ToList()
            });
        }

        return RedirectBack(returnUrl);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Checkout(Guid? buyNowVariantId)
    {
        var cart = GetCart();
        List<CartItemViewModel> checkoutItems = new List<CartItemViewModel>();

        if (buyNowVariantId.HasValue && buyNowVariantId.Value != Guid.Empty)
        {
            var variantResponse = await Api.GetAsync($"api/productvariants/{buyNowVariantId.Value}");
            if (variantResponse.IsSuccessStatusCode)
            {
                var variant = await variantResponse.Content.ReadFromJsonAsync<ProductVariantCatalogViewModel>();
                if (variant != null && variant.StockQuantity > 0)
                {
                    var product = await Api.GetFromJsonAsync<ProductCatalogViewModel>($"api/products/{variant.ProductId}");
                    checkoutItems.Add(new CartItemViewModel
                    {
                        ProductVariantId = variant.ProductVariantId,
                        ProductId = variant.ProductId,
                        Name = product?.Name ?? "Sản phẩm",
                        VariantName = string.Join(" - ", variant.VariantValues.Select(vv => $"{vv.OptionName}: {vv.ValueName}")),
                        UnitPrice = (product?.Price ?? 0) + variant.Price,
                        Image = product?.Image,
                        Quantity = (byte)1
                    });
                }
            }
        }
        else
        {
            checkoutItems = cart.Items;
        }

        if (!checkoutItems.Any())
        {
            TempData["CartError"] = "Giỏ hàng của bạn đang trống.";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.CheckoutItems = checkoutItems;
        ViewBag.BuyNowVariantId = buyNowVariantId;
        
        var model = new CheckoutViewModel();
        
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var userResponse = await Api.GetAsync($"api/users/{userId}");
            if (userResponse.IsSuccessStatusCode)
            {
                var userDoc = await userResponse.Content.ReadFromJsonAsync<JsonDocument>();
                if (userDoc != null)
                {
                    model.CustomerName = userDoc.RootElement.GetProperty("fullName").GetString() ?? User.Identity.Name;
                    model.CustomerPhone = userDoc.RootElement.GetProperty("phoneNumber").GetString() ?? "";
                }
            }
            else
            {
                model.CustomerName = User.Identity.Name;
            }
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, string? returnUrl = null)
    {
        List<CartItemViewModel> checkoutItems;

        if (model.BuyNowProductVariantId.HasValue && model.BuyNowProductVariantId.Value != Guid.Empty)
        {
            // We need to fetch the product by some means, or we can just fetch all and find the variant, but since we don't have productId, we might need a direct variant API or we just use cart items.
            // Wait, if it's BuyNow, the frontend should send BuyNowProductId as well or we just lookup the variant.
            // Since we didn't add BuyNowProductId to CheckoutViewModel, we have to look up the variant.
            // But we don't have an endpoint for a single variant in MVC API helper unless we query the api/productvariants.
            var variantResponse = await Api.GetAsync($"api/productvariants/{model.BuyNowProductVariantId.Value}");
            if (!variantResponse.IsSuccessStatusCode)
            {
                TempData["CartError"] = "Sản phẩm không tồn tại hoặc đã hết hàng.";
                return RedirectBack(returnUrl);
            }
            
            var variant = await variantResponse.Content.ReadFromJsonAsync<ProductVariantCatalogViewModel>();
            if (variant == null || variant.StockQuantity <= 0)
            {
                TempData["CartError"] = "Sản phẩm không tồn tại hoặc đã hết hàng.";
                return RedirectBack(returnUrl);
            }

            var product = await Api.GetFromJsonAsync<ProductCatalogViewModel>($"api/products/{variant.ProductId}");

            checkoutItems = new List<CartItemViewModel>
            {
                new CartItemViewModel
                {
                    ProductVariantId = variant.ProductVariantId,
                    ProductId = variant.ProductId,
                    Name = product?.Name ?? "Sản phẩm",
                    VariantName = string.Join(" - ", variant.VariantValues.Select(vv => $"{vv.OptionName}: {vv.ValueName}")),
                    UnitPrice = (product?.Price ?? 0) + variant.Price,
                    Image = product?.Image,
                    Quantity = (byte)1
                }
            };
        }
        else
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                TempData["CartError"] = "Giỏ hàng đang trống.";
                return RedirectBack(returnUrl);
            }
            checkoutItems = cart.Items;
        }

        if (!ModelState.IsValid)
        {
            TempData["CartError"] = "Vui lòng nhập đầy đủ thông tin giao hàng.";
            return RedirectBack(returnUrl);
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var request = new ApiOrderCreateRequest
        {
            UserId = userId,
            CustomerName = model.CustomerName,
            CustomerPhone = model.CustomerPhone,
            PaymentMethod = model.PaymentMethod,
            ShippingAddress = model.CustomerAddress,
            VoucherCode = model.VoucherCode,
            Items = checkoutItems.Select(item => new ApiOrderItemRequest
            {
                ProductVariantId = item.ProductVariantId,
                Quantity = item.Quantity
            }).ToList()
        };

        var response = await Api.PostAsJsonAsync("api/orders", request);
        if (!response.IsSuccessStatusCode)
        {
            TempData["CartError"] = await ReadApiMessage(response);
            return RedirectBack(returnUrl);
        }

        if (!model.BuyNowProductVariantId.HasValue || model.BuyNowProductVariantId.Value == Guid.Empty)
        {
            HttpContext.Session.Remove(CartSessionKey);
        }
        
        TempData["CartMessage"] = "Đặt hàng thành công. Đơn hàng của bạn đã được ghi nhận.";
        return RedirectToAction("Index", "Orders");
    }

    [HttpPost]
    public async Task<IActionResult> ValidateVoucher([FromBody] ValidateVoucherViewModel model)
    {
        var response = await Api.PostAsJsonAsync("api/vouchers/validate", model);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<object>();
            return Json(content);
        }
        else
        {
            var error = await ReadApiMessage(response);
            return Json(new { isValid = false, message = error });
        }
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
