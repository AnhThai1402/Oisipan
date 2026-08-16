using FrontendMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FrontendMvc.Controllers
{
    [Authorize]
    public class VouchersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VouchersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userIdInt))
            {
                return Unauthorized();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("OisipanApi");
                var response = await client.GetAsync($"/api/orders/user/{userIdInt}/vouchers");
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var content = await response.Content.ReadAsStringAsync();
                var vouchers = System.Text.Json.JsonSerializer.Deserialize<List<UserVoucherDto>>(
                    content, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<UserVoucherDto>();

                return View(vouchers);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Không thể tải danh sách voucher: {ex.Message}");
                return View(new List<UserVoucherDto>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveVoucher(string code)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userIdGuid) || string.IsNullOrWhiteSpace(code))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("OisipanApi");
                var requestBody = new { UserId = userIdGuid, Code = code.Trim() };
                var response = await client.PostAsJsonAsync("/api/vouchers/assign", requestBody);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Lưu mã giảm giá thành công.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorContent);
                        if (errorObj.TryGetProperty("message", out var msgElement))
                        {
                            TempData["ErrorMessage"] = msgElement.GetString();
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Không thể lưu mã giảm giá.";
                        }
                    }
                    catch
                    {
                        TempData["ErrorMessage"] = "Không thể lưu mã giảm giá.";
                    }
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lưu mã giảm giá.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
