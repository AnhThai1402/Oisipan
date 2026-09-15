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
    }
}
