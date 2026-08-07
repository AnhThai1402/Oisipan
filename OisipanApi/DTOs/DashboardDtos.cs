namespace Oishipan.DTOs;

public class DashboardStatsDto
{
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int TotalCustomers { get; set; }
    public List<OrderTodayDto> RecentOrders { get; set; } = new();
    public List<DailyRevenueDto> DailyRevenues { get; set; } = new();
}

public class DailyRevenueDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class OrderTodayDto
{
    public Guid OrderId { get; set; }
    public string OrderCode => $"OP-{OrderId:0000}";
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string EstimatedDelivery { get; set; } = string.Empty;
}
