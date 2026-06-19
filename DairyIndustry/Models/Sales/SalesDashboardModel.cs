namespace DairyIndustry.Models
{
    public class SalesDashboardSummaryModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TotalDistributors { get; set; }
    }

    public class OrderStatusCountModel
    {
        public string? OrderStatus { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DistributorSalesModel
    {
        public int DistributorId { get; set; }
        public string? DistributorName { get; set; }
        public string? Location { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int DeliveredOrders { get; set; }
        public int PendingOrders { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public class SalesDashboardViewModel
    {
        public SalesDashboardSummaryModel Summary { get; set; } = new();
        public List<OrderStatusCountModel> OrdersByStatus { get; set; } = new();
        public List<SalesOrderModel> RecentOrders { get; set; } = new();
        public List<DistributorSalesModel> TopDistributors { get; set; } = new();
    }
}