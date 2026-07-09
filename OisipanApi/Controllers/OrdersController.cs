using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Elements;
using System.IO;
using ZXing;
using ZXing.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
    private readonly IConfiguration _config;

    public OrdersController(OishipanContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
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
        // Lấy các voucher đã được gán/sử dụng bởi user
        var userVouchers = await _context.UserVouchers
            .Where(uv => uv.UserId == userId)
            .Include(uv => uv.Voucher)
            .ToListAsync();

        // Lấy các voucher public đang active
        var publicVouchers = await _context.Vouchers
            .Where(v => v.VoucherType == "Public" && v.Status == "Active" && v.StartDate <= DateTime.Now)
            .ToListAsync();

        var uniqueUserVouchers = userVouchers
            .GroupBy(uv => uv.VoucherId)
            .Select(g => g.OrderBy(uv => uv.IsUsed).ThenByDescending(uv => uv.AssignedDate).First())
            .ToList();

        var result = new List<UserVoucherDto>();

        foreach(var uv in uniqueUserVouchers)
        {
            if (uv.Voucher != null)
            {
                result.Add(new UserVoucherDto
                {
                    UserVoucherId = uv.UserVoucherId,
                    UserId = uv.UserId,
                    VoucherId = uv.VoucherId,
                    VoucherCode = uv.Voucher.Code,
                    DiscountValue = uv.Voucher.DiscountValue,
                    DiscountType = uv.Voucher.DiscountType,
                    MinimumItems = uv.Voucher.MinimumItems,
                    ExpiryDate = uv.Voucher.ExpiryDate,
                    IsUsed = uv.IsUsed,
                    UsedDate = uv.UsedDate,
                    AssignedDate = uv.AssignedDate
                });
            }
        }

        var userVoucherIds = uniqueUserVouchers.Select(uv => uv.VoucherId).ToHashSet();
        foreach(var pv in publicVouchers)
        {
            if (!userVoucherIds.Contains(pv.VoucherId))
            {
                result.Add(new UserVoucherDto
                {
                    UserVoucherId = 0,
                    UserId = userId,
                    VoucherId = pv.VoucherId,
                    VoucherCode = pv.Code,
                    DiscountValue = pv.DiscountValue,
                    DiscountType = pv.DiscountType,
                    MinimumItems = pv.MinimumItems,
                    ExpiryDate = pv.ExpiryDate,
                    IsUsed = false,
                    UsedDate = null,
                    AssignedDate = pv.StartDate
                });
            }
        }

        return Ok(result.OrderByDescending(v => v.AssignedDate));
    }

    [HttpGet("{id:int}/invoice.pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id)
    {
        try
        {
            // Ensure QuestPDF license is configured
            QuestPDF.Settings.License = LicenseType.Community;

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == order.UserId);
            var model = await ToAdminResponse(order, account);

            // Generate PDF using QuestPDF
            DocumentMetadata meta = new DocumentMetadata { Title = $"Invoice_OP_{model.OrderId:0000}" };

            byte[] pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Oisipan").FontSize(20).SemiBold().FontColor(Colors.Black);
                        col.Item().Text("Địa chỉ: Số 1, Phố A, Thành phố").FontSize(10).FontColor(Colors.Grey.Darken2);
                        col.Item().Text("Hotline: 0123-456-789").FontSize(10).FontColor(Colors.Grey.Darken2);
                    });
                    row.ConstantItem(160).AlignRight().Column(col =>
                    {
                        col.Item().Text($"HÓA ĐƠN BÁN HÀNG").FontSize(14).SemiBold();
                        col.Item().Text($"Mã: OP-{model.OrderId:0000}").FontSize(12);
                        col.Item().Text($"Ngày: {model.OrderDate:dd/MM/yyyy HH:mm}").FontSize(12);
                    });
                });

                // Prepare barcode image bytes (Code128)
                byte[] barcodeBytes = Array.Empty<byte>();
                try
                {
                    var writer = new BarcodeWriterPixelData
                    {
                        Format = BarcodeFormat.CODE_128,
                        Options = new EncodingOptions
                        {
                            Height = 60,
                            Width = 300,
                            Margin = 2
                        }
                    };
                    var pixel = writer.Write($"OP-{model.OrderId:0000}");
                    try
                    {
                        var width = pixel.Width;
                        var height = pixel.Height;
                        var src = pixel.Pixels; // expected RGB24
                        var rgba = new Rgba32[width * height];
                        for (int i = 0, p = 0; i < rgba.Length; i++, p += 3)
                        {
                            rgba[i] = new Rgba32(src[p], src[p + 1], src[p + 2], 255);
                        }
                        using var img = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(rgba, width, height);
                        using var msBar = new MemoryStream();
                        img.SaveAsPng(msBar);
                        barcodeBytes = msBar.ToArray();
                    }
                    catch
                    {
                        barcodeBytes = Array.Empty<byte>();
                    }
                }
                catch
                {
                    barcodeBytes = Array.Empty<byte>();
                }

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c=>{
                            c.Item().Text("Khách hàng").SemiBold();
                            c.Item().Text(model.CustomerName);
                            c.Item().Text(model.CustomerPhone);
                            c.Item().Text(model.ShippingAddress);
                        });
                        r.ConstantItem(160).Column(c=>{
                            c.Item().Text("Thanh toán").SemiBold();
                            c.Item().Text($"Phương thức: {model.PaymentMethod}");
                            c.Item().Text($"Trạng thái: {model.Status}");
                        });
                    });

                    col.Item().Element(e =>
                    {
                        e.Container().Background(Colors.Grey.Lighten3).Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("Sản phẩm").SemiBold();
                            r.ConstantItem(50).Text("SL").SemiBold().AlignCenter();
                            r.ConstantItem(100).Text("Giá").SemiBold().AlignRight();
                            r.ConstantItem(100).Text("Thành tiền").SemiBold().AlignRight();
                        });
                    });

                    foreach(var item in model.Items)
                    {
                        col.Item().Row(r=>{
                            r.RelativeItem().Text(item.ProductName);
                            r.ConstantItem(50).AlignCenter().Text(item.Quantity.ToString());
                            r.ConstantItem(100).AlignRight().Text(item.Price.ToString("N0") + "đ");
                            r.ConstantItem(100).AlignRight().Text((item.Price*item.Quantity).ToString("N0") + "đ");
                        });
                    }

                    col.Item().Row(r=>{
                        r.RelativeItem().Column(cc=>{
                            cc.Item().Text($"Tạm tính: {model.TotalAmount:N0}đ");
                            if(model.DiscountAmount > 0) cc.Item().Text($"Giảm giá: -{model.DiscountAmount:N0}đ");
                        });
                        r.ConstantItem(320).AlignRight().Column(cc=>{
                            cc.Item().Text($"Tổng: {model.FinalAmount:N0}đ").FontSize(14).SemiBold();
                            if(barcodeBytes.Length>0) cc.Item().Element(e => e.Image(barcodeBytes));
                        });
                    });
                });

                page.Footer().AlignCenter().Text("Cảm ơn quý khách! Hẹn gặp lại.");
            });
        }).GeneratePdf();

        // Save to wwwroot/invoices
        try
        {
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var invoicesDir = Path.Combine(webRoot, "invoices");
            if (!Directory.Exists(invoicesDir)) Directory.CreateDirectory(invoicesDir);
            var fileName = $"invoice-OP-{model.OrderId:0000}.pdf";
            var filePath = Path.Combine(invoicesDir, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
        }
        catch
        {
            // ignore save errors
        }

            var downloadName = $"OP-{model.OrderId:0000}-invoice.pdf";
            return File(pdfBytes, "application/pdf", downloadName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF generation error: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { message = "Lỗi khi tạo PDF hóa đơn: " + ex.Message });
        }
    }

    [HttpPost("{id:int}/send-invoice")]
    public async Task<IActionResult> SendInvoice(int id, [FromBody] SendInvoiceRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == order.UserId);
        var model = await ToAdminResponse(order, account);

        // generate pdf bytes using same inline generator
        DocumentMetadata meta = new DocumentMetadata { Title = $"Invoice_OP_{model.OrderId:0000}" };

        byte[] pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Oisipan").FontSize(20).SemiBold().FontColor(Colors.Black);
                        col.Item().Text("Địa chỉ: Số 1, Phố A, Thành phố").FontSize(10).FontColor(Colors.Grey.Darken2);
                        col.Item().Text("Hotline: 0123-456-789").FontSize(10).FontColor(Colors.Grey.Darken2);
                    });
                    row.ConstantItem(160).AlignRight().Column(col =>
                    {
                        col.Item().Text($"HÓA ĐƠN BÁN HÀNG").FontSize(14).SemiBold();
                        col.Item().Text($"Mã: OP-{model.OrderId:0000}").FontSize(12);
                        col.Item().Text($"Ngày: {model.OrderDate:dd/MM/yyyy HH:mm}").FontSize(12);
                    });
                });

                // Prepare barcode image bytes (Code128)
                byte[] barcodeBytes = Array.Empty<byte>();
                try
                {
                    var writer = new BarcodeWriterPixelData
                    {
                        Format = BarcodeFormat.CODE_128,
                        Options = new EncodingOptions
                        {
                            Height = 60,
                            Width = 300,
                            Margin = 2
                        }
                    };
                    var pixel = writer.Write($"OP-{model.OrderId:0000}");
                    try
                    {
                        var width = pixel.Width;
                        var height = pixel.Height;
                        var src = pixel.Pixels; // expected RGB24
                        var rgba = new Rgba32[width * height];
                        for (int i = 0, p = 0; i < rgba.Length; i++, p += 3)
                        {
                            rgba[i] = new Rgba32(src[p], src[p + 1], src[p + 2], 255);
                        }
                        using var img = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(rgba, width, height);
                        using var msBar = new MemoryStream();
                        img.SaveAsPng(msBar);
                        barcodeBytes = msBar.ToArray();
                    }
                    catch
                    {
                        barcodeBytes = Array.Empty<byte>();
                    }
                }
                catch
                {
                    barcodeBytes = Array.Empty<byte>();
                }

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c=>{
                            c.Item().Text("Khách hàng").SemiBold();
                            c.Item().Text(model.CustomerName);
                            c.Item().Text(model.CustomerPhone);
                            c.Item().Text(model.ShippingAddress);
                        });
                        r.ConstantItem(160).Column(c=>{
                            c.Item().Text("Thanh toán").SemiBold();
                            c.Item().Text($"Phương thức: {model.PaymentMethod}");
                            c.Item().Text($"Trạng thái: {model.Status}");
                        });
                    });

                    col.Item().Element(e =>
                    {
                        e.Container().Background(Colors.Grey.Lighten3).Padding(6).Row(r =>
                        {
                            r.RelativeItem().Text("Sản phẩm").SemiBold();
                            r.ConstantItem(50).Text("SL").SemiBold().AlignCenter();
                            r.ConstantItem(100).Text("Giá").SemiBold().AlignRight();
                            r.ConstantItem(100).Text("Thành tiền").SemiBold().AlignRight();
                        });
                    });

                    foreach(var item in model.Items)
                    {
                        col.Item().Row(r=>{
                            r.RelativeItem().Text(item.ProductName);
                            r.ConstantItem(50).AlignCenter().Text(item.Quantity.ToString());
                            r.ConstantItem(100).AlignRight().Text(item.Price.ToString("N0") + "đ");
                            r.ConstantItem(100).AlignRight().Text((item.Price*item.Quantity).ToString("N0") + "đ");
                        });
                    }

                    col.Item().Row(r=>{
                        r.RelativeItem().Column(cc=>{
                            cc.Item().Text($"Tạm tính: {model.TotalAmount:N0}đ");
                            if(model.DiscountAmount > 0) cc.Item().Text($"Giảm giá: -{model.DiscountAmount:N0}đ");
                        });
                        r.ConstantItem(320).AlignRight().Column(cc=>{
                            cc.Item().Text($"Tổng: {model.FinalAmount:N0}đ").FontSize(14).SemiBold();
                            if(barcodeBytes.Length>0) cc.Item().Element(e => e.Image(barcodeBytes));
                        });
                    });
                });

                page.Footer().AlignCenter().Text("Cảm ơn quý khách! Hẹn gặp lại.");
            });
        }).GeneratePdf();

        // Save to wwwroot/invoices
        string savedRelativePath = string.Empty;
        InvoiceRecord rec = new InvoiceRecord { OrderId = model.OrderId, CreatedDate = DateTime.Now };
        try
        {
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var invoicesDir = Path.Combine(webRoot, "invoices");
            if (!Directory.Exists(invoicesDir)) Directory.CreateDirectory(invoicesDir);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"invoice-OP-{model.OrderId:0000}-{timestamp}.pdf";
            var filePath = Path.Combine(invoicesDir, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
            savedRelativePath = Path.Combine("invoices", fileName);
            if (request.SaveCopy)
            {
                rec.FilePath = savedRelativePath;
                rec.Email = request.Email;
                _context.InvoiceRecords.Add(rec);
                await _context.SaveChangesAsync();
            }
        }
        catch
        {
        }

        if (request.AttachPdf && !string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                var smtpSection = _config.GetSection("Smtp");
                var host = smtpSection.GetValue<string>("Host");
                var port = smtpSection.GetValue<int>("Port");
                var user = smtpSection.GetValue<string>("User");
                var pass = smtpSection.GetValue<string>("Pass");
                var from = smtpSection.GetValue<string>("From");

                using var msg = new System.Net.Mail.MailMessage();
                msg.From = new System.Net.Mail.MailAddress(from ?? user ?? "noreply@example.com", "Oisipan");
                msg.To.Add(request.Email);
                msg.Subject = $"Hóa đơn OP-{model.OrderId:0000}";
                msg.Body = string.IsNullOrWhiteSpace(request.Message) ? "Vui lòng xem hóa đơn đính kèm." : request.Message;
                msg.IsBodyHtml = false;

                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    var ms = new MemoryStream(pdfBytes);
                    var attach = new System.Net.Mail.Attachment(ms, $"OP-{model.OrderId:0000}-invoice.pdf", "application/pdf");
                    msg.Attachments.Add(attach);
                }

                using var client = new System.Net.Mail.SmtpClient(host, port);
                client.EnableSsl = smtpSection.GetValue<bool>("EnableSsl");
                if (!string.IsNullOrEmpty(user)) client.Credentials = new System.Net.NetworkCredential(user, pass);
                client.Send(msg);

                // mark as sent
                if (request.SaveCopy)
                {
                    rec.EmailSent = true;
                    rec.SentAt = DateTime.Now;
                    _context.InvoiceRecords.Update(rec);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Gửi email thất bại.", detail = ex.Message });
            }
        }

        return Ok(new { message = "Invoice processed.", path = savedRelativePath });
    }

    private IQueryable<Order> BuildOrderQuery()
    {
        return _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.Account);
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
        var customer = account ?? await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == order.UserId);

        return new AdminOrderResponse
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            CustomerName = !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : customer?.FullName,
            CustomerEmail = customer?.Email,
            CustomerPhone = !string.IsNullOrWhiteSpace(order.CustomerPhone) ? order.CustomerPhone : customer?.PhoneNumber,
            ShippingAddress = order.ShippingAddress,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            DiscountAmount = 0,
            FinalAmount = order.TotalAmount,
            VoucherCode = null,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            UpdatedDate = null,
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

    private Task AssignVoucherToUser(int userId)
    {
        return Task.CompletedTask;
    }
}
