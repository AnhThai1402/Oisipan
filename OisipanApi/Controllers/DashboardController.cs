using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly OishipanContext _context;

    public DashboardController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        // Today's revenue
        var todayOrders = await _context.Orders
            .Where(o => o.OrderDate.Date == today && o.Status != "Đã hủy")
            .ToListAsync();
        var todayRevenue = todayOrders.Sum(o => o.TotalAmount);

        // Month revenue
        var monthOrders = await _context.Orders
            .Where(o => o.OrderDate >= monthStart && o.Status != "Đã hủy")
            .ToListAsync();
        var monthRevenue = monthOrders.Sum(o => o.TotalAmount);

        // Order counts
        var pendingOrders = await _context.Orders
            .CountAsync(o => o.Status == "Chờ xác nhận");
        var confirmedOrders = await _context.Orders
            .CountAsync(o => o.Status == "Đã xác nhận" || o.Status == "Đang chuẩn bị");

        // Stock info
        var lowStockThreshold = 10;
        var lowStockProducts = await _context.Products
            .CountAsync(p => p.Quantity > 0 && p.Quantity <= lowStockThreshold);
        var outOfStockProducts = await _context.Products
            .CountAsync(p => p.Quantity <= 0);

        // Customer info
        var newCustomersThisMonth = await _context.Accounts
            .CountAsync(a => a.Status);
        var totalCustomers = await _context.Accounts
            .CountAsync(a => a.Status);

        // Recent orders
        var recentOrders = await _context.Orders
            .Include(o => o.Account)
            .Include(o => o.OrderDetails)
            .Where(o => o.OrderDate.Date == today)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => new OrderTodayDto
            {
                OrderId = o.OrderId,
                CustomerName = o.Account == null ? "Khách hàng" : o.Account.FullName,
                CustomerPhone = o.Account == null ? "N/A" : o.Account.PhoneNumber,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod,
                Status = o.Status,
                CreatedDate = o.OrderDate,
                EstimatedDelivery = GetEstimatedDelivery(o.Status)
            })
            .ToListAsync();

        // Daily revenue breakdown for the month
        var dailyRevenues = monthOrders
            .GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key.ToString("dd/MM"),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .ToList();

        return Ok(new DashboardStatsDto
        {
            TodayRevenue = todayRevenue,
            MonthRevenue = monthRevenue,
            PendingOrders = pendingOrders,
            ConfirmedOrders = confirmedOrders,
            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts,
            NewCustomersThisMonth = newCustomersThisMonth,
            TotalCustomers = totalCustomers,
            RecentOrders = recentOrders,
            DailyRevenues = dailyRevenues
        });
    }

    private static string GetEstimatedDelivery(string status)
    {
        return status switch
        {
            "Chờ xác nhận" => "Hôm nay",
            "Đã xác nhận" => "Hôm nay",
            "Đang chuẩn bị" => "Hôm nay",
            "Đang giao" => "35-60 phút",
            "Đã giao" => "Đã giao",
            "Đã hủy" => "Đã hủy",
            _ => "Chưa rõ"
        };
    }
}
