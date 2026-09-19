using System.Security.Claims;
using FrontendMvc.Extensions;
using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    public WishlistController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<IActionResult> Index()
    {
        if (!TryGetUserId(out var userId)) return Challenge();
        var items = await Api.GetFromJsonAsyncWithOptions<List<WishlistItemViewModel>>($"api/wishlist/{userId}") ?? new();
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid productId, string? returnUrl = null)
    {
        if (!TryGetUserId(out var userId)) return Challenge();
        var existing = await Api.GetFromJsonAsyncWithOptions<List<WishlistItemViewModel>>($"api/wishlist/{userId}") ?? new();
        var response = existing.Any(i => i.ProductId == productId)
            ? await Api.DeleteAsync($"api/wishlist/{userId}/{productId}")
            : await Api.PostAsync($"api/wishlist/{userId}/{productId}", null);
        TempData[response.IsSuccessStatusCode ? "CartMessage" : "CartError"] = response.IsSuccessStatusCode ? "Đã cập nhật wishlist." : "Không thể cập nhật wishlist.";
        return Redirect(returnUrl ?? Url.Action(nameof(Index))!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(Guid productId)
    {
        if (!TryGetUserId(out var userId)) return Challenge();

        var wishlistItems = await Api.GetFromJsonAsyncWithOptions<List<WishlistItemViewModel>>($"api/wishlist/{userId}") ?? new();
        var wishlistItem = wishlistItems.FirstOrDefault(item => item.ProductId == productId);

        if (wishlistItem is null || wishlistItem.StockQuantity <= 0)
        {
            TempData["CartError"] = "Sản phẩm không còn khả dụng hoặc đã bị bỏ khỏi wishlist.";
            return RedirectToAction(nameof(Index));
        }

        var cart = HttpContext.Session.GetJson<List<CartItemViewModel>>("Cart") ?? new();
        if (!cart.Any(i => i.ProductId == productId))
        {
            cart.Add(new CartItemViewModel
            {
                ProductId = productId,
                ProductName = wishlistItem.ProductName,
                UnitPrice = wishlistItem.Price,
                Image = wishlistItem.Image,
                Quantity = 1
            });
        }

        HttpContext.Session.SetJson("Cart", cart);
        TempData["CartMessage"] = "Đã chuyển sản phẩm vào giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    private HttpClient Api => _httpClientFactory.CreateClient("OisipanApi");
}