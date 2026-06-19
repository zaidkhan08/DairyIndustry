namespace DairyIndustry.Models
{
    // ── Monthly spend data point (for bar chart) ─────────────────────────
    public class MonthlySpendModel
    {
        public string MonthLabel { get; set; } = "";   // e.g. "Jan 25"
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
    }

    // ── Top product row (for top-5 table) ────────────────────────────────
    public class TopProductModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductType { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
    }

    // ── Combined ViewModel for DistributorDashboard view ─────────────────
    public class DistributorDashboardViewModel
    {
        // Stat cards
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }   // Delivered + Received
        public int CancelledOrders { get; set; }
        public decimal TotalSpent { get; set; }    // sum of Delivered + Received

        // Bar chart — last 6 months spend
        public List<MonthlySpendModel> MonthlySpend { get; set; } = new();

        // Doughnut chart — order counts by status
        public List<OrderStatusCountModel> OrdersByStatus { get; set; } = new();

        // Top 5 products table
        public List<TopProductModel> TopProducts { get; set; } = new();

        // Recent 5 orders table
        public List<SalesOrderModel> RecentOrders { get; set; } = new();
    }
}