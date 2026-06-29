using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Chờ xác nhận",
        "Đã xác nhận",
        "Đang chuẩn bị",
        "Đang giao",
        "Đã giao",
        "Đã hủy"
    };

    private readonly OishipanContext _context;

    public OrdersController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
    {
        var ordersData = await BuildOrderQuery()
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var orders = ordersData.Select(o => ToResponse(o)).ToList();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var order = await BuildOrderQuery()
            .FirstOrDefaultAsync(o => o.OrderId == id);

        return order is null
            ? NotFound(new { message = "Không tìm thấy đơn hàng." })
            : Ok(ToResponse(order));
    }

    [HttpGet("admin/orders")]
    public async Task<ActionResult<IEnumerable<AdminOrderResponse>>> GetAllForAdmin()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var adminOrders = new List<AdminOrderResponse>();
        foreach (var order in orders)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == order.UserId);
            adminOrders.Add(await ToAdminResponse(order, account));
        }

        return Ok(adminOrders);
    }

    [HttpGet("admin/{id:int}")]
    public async Task<ActionResult<AdminOrderResponse>> GetByIdForAdmin(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            return NotFound(new { message = "Không tìm thấy đơn hàng." });
        }

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == order.UserId);
        return Ok(await ToAdminResponse(order, account));
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetByUser(int userId)
    {
        var ordersData = await BuildOrderQuery()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var orders = ordersData.Select(o => ToResponse(o)).ToList();

        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(OrderCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await _context.Accounts.AnyAsync(a => a.UserId == request.UserId && a.Status))
        {
            return BadRequest(new { message = "Tài khoản đặt hàng không tồn tại hoặc đã bị khóa." });
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                return BadRequest(new { message = $"Sản phẩm #{item.ProductId} không tồn tại." });
            }

            if (product.Quantity < item.Quantity)
            {
                return BadRequest(new { message = $"{product.Name} chỉ còn {product.Quantity} sản phẩm." });
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = new Order
        {
            UserId = request.UserId,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            OrderDate = DateTime.Now,
            Status = "Chờ xác nhận",
            PaymentMethod = request.PaymentMethod.Trim(),
            ShippingAddress = request.ShippingAddress.Trim()
        };

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            product.Quantity -= item.Quantity;

            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.ProductId,
                Price = product.Price,
                Quantity = item.Quantity,
                Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim()
            });
        }

        order.TotalAmount = order.OrderDetails.Sum(item => item.Price * item.Quantity);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assign voucher to user after successful order
        await AssignVoucherToUser(request.UserId);

        var created = await BuildOrderQuery()
            .FirstAsync(o => o.OrderId == order.OrderId);

        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, ToResponse(created));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatusUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var status = request.Status.Trim();
        if (!AllowedStatuses.Contains(status))
        {
            return BadRequest(new { message = "Trạng thái đơn hàng không hợp lệ." });
        }

        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound(new { message = "Không tìm thấy đơn hàng." });
        }

        // Only allow cancellation from "Chờ xác nhận" or "Đã xác nhận" status
        if (status == "Đã hủy")
        {
            var allowedCancellationStatuses = new[] { "Chờ xác nhận", "Đã xác nhận" };
            if (!allowedCancellationStatuses.Contains(order.Status))
            {
                return BadRequest(new { message = $"Chỉ có thể hủy đơn hàng ở trạng thái 'Chờ xác nhận' hoặc 'Đã xác nhận'. Trạng thái hiện tại: '{order.Status}'." });
            }
        }

        order.Status = status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/cancellation-request")]
    public async Task<IActionResult> RequestCancellation(int id, OrderCancellationRequestCreateDto request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound(new { message = "Không tìm thấy đơn hàng." });
        }

        // Check if order can be cancelled
        if (order.Status == "Đã giao" || order.Status == "Đã hủy")
        {
            return BadRequest(new { message = $"Không thể hủy đơn hàng ở trạng thái '{order.Status}'." });
        }

        // Check if cancellation request already exists
        var existingRequest = await _context.OrderCancellationRequests
            .FirstOrDefaultAsync(r => r.OrderId == id && r.Status == "Pending");

        if (existingRequest is not null)
        {
            return BadRequest(new { message = "Đã có yêu cầu hủy đơn chờ xử lý. Vui lòng chờ phản hồi từ admin." });
        }

        var cancellationRequest = new OrderCancellationRequest
        {
            OrderId = id,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            RequestDate = DateTime.Now
        };

        _context.OrderCancellationRequests.Add(cancellationRequest);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Yêu cầu hủy đơn hàng đã được gửi. Vui lòng chờ phản hồi từ admin." });
    }

    [HttpGet("{id:int}/cancellation-requests")]
    public async Task<ActionResult<IEnumerable<OrderCancellationRequestDto>>> GetCancellationRequests(int id)
    {
        var requests = await _context.OrderCancellationRequests
            .Where(r => r.OrderId == id)
            .OrderByDescending(r => r.RequestDate)
            .Select(r => new OrderCancellationRequestDto
            {
                CancellationRequestId = r.CancellationRequestId,
                OrderId = r.OrderId,
                Reason = r.Reason,
                Status = r.Status,
                RequestDate = r.RequestDate,
                ResponseDate = r.ResponseDate,
                AdminNote = r.AdminNote
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPatch("cancellation-request/{id:int}/respond")]
    public async Task<IActionResult> RespondToCancellationRequest(int id, [FromBody] AdminCancellationResponseDto response)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var request = await _context.OrderCancellationRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu hủy đơn." });
        }

        request.Status = response.IsApproved ? "Approved" : "Rejected";
        request.AdminNote = response.AdminNote?.Trim();
        request.ResponseDate = DateTime.Now;

        if (response.IsApproved)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order is not null)
            {
                order.Status = "Đã hủy";
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = request.Status == "Approved" ? "Yêu cầu hủy đơn hàng đã được phê duyệt." : "Yêu cầu hủy đơn hàng đã bị từ chối." });
    }

    [HttpGet("user/{userId:int}/vouchers")]
    public async Task<ActionResult<IEnumerable<UserVoucherDto>>> GetUserVouchers(int userId)
    {
        var vouchers = await _context.UserVouchers
            .Where(uv => uv.UserId == userId)
            .Include(uv => uv.Voucher)
            .OrderByDescending(uv => uv.AssignedDate)
            .Select(uv => new UserVoucherDto
            {
                UserVoucherId = uv.UserVoucherId,
                UserId = uv.UserId,
                VoucherId = uv.VoucherId,
                VoucherCode = uv.Voucher!.Code,
                DiscountValue = uv.Voucher.DiscountValue,
                MinimumItems = uv.Voucher.MinimumItems,
                ExpiryDate = uv.Voucher.ExpiryDate,
                IsUsed = uv.IsUsed,
                UsedDate = uv.UsedDate,
                AssignedDate = uv.AssignedDate
            })
            .ToListAsync();

        return Ok(vouchers);
    }

    private async Task AssignVoucherToUser(int userId)
    {
        try
        {
            // Create or get the reward voucher for purchases
            const string rewardVoucherCode = "NEXTBUY";
            
            var rewardVoucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == rewardVoucherCode && v.Status == "Active");

            if (rewardVoucher is null)
            {
                // Create reward voucher if not exists
                rewardVoucher = new Voucher
                {
                    Code = rewardVoucherCode,
                    DiscountValue = 30000, // 30k discount for next purchase
                    MinimumItems = 0, // No minimum items required
                    ExpiryDate = DateTime.Now.AddDays(90), // Valid for 90 days
                    CreatedDate = DateTime.Now,
                    Status = "Active"
                };
                _context.Vouchers.Add(rewardVoucher);
                await _context.SaveChangesAsync();
            }

            // Check if user already has this voucher (can have multiple if already used some)
            var userVoucher = new UserVoucher
            {
                UserId = userId,
                VoucherId = rewardVoucher.VoucherId,
                AssignedDate = DateTime.Now,
                IsUsed = false
            };

            _context.UserVouchers.Add(userVoucher);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log exception but don't fail the order creation
            Console.WriteLine($"Error assigning voucher to user {userId}: {ex.Message}");
        }
    }

    private IQueryable<Order> BuildOrderQuery()
    {
        return _context.Orders
            .Include(o => o.Account)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product);
    }

    private static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            CustomerName = !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : order.Account?.FullName,
            CustomerPhone = !string.IsNullOrWhiteSpace(order.CustomerPhone) ? order.CustomerPhone : order.Account?.PhoneNumber,
            ShippingAddress = order.ShippingAddress,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            Items = order.OrderDetails.Select(item => new OrderDetailResponse
            {
                OrderDetailId = item.OrderDetailId,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name,
                Price = item.Price,
                Quantity = item.Quantity,
                Note = item.Note
            }).ToList()
        };
    }

    private async Task<AdminOrderResponse> ToAdminResponse(Order order, Account? account = null)
    {
        // Get voucher information if used in this order
        var userVoucher = await _context.UserVouchers
            .Include(uv => uv.Voucher)
            .FirstOrDefaultAsync(uv => uv.UsedInOrderId == order.OrderId);

        var voucherCode = userVoucher?.Voucher?.Code;
        var discountAmount = userVoucher?.Voucher?.DiscountValue ?? 0;
        var finalAmount = order.TotalAmount - discountAmount;

        return new AdminOrderResponse
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            CustomerName = !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : account?.FullName,
            CustomerEmail = account?.Email,
            CustomerPhone = !string.IsNullOrWhiteSpace(order.CustomerPhone) ? order.CustomerPhone : account?.PhoneNumber,
            ShippingAddress = order.ShippingAddress,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            VoucherCode = voucherCode,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            UpdatedDate = null, // Orders don't have UpdatedDate field in current schema
            Notes = null,
            Items = order.OrderDetails.Select(item => new AdminOrderItemResponse
            {
                OrderDetailId = item.OrderDetailId,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name,
                Price = item.Price,
                Quantity = item.Quantity,
                Note = item.Note
            }).ToList()
        };
    }
}
