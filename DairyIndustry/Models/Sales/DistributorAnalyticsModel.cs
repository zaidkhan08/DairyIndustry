namespace DairyIndustry.Models
{
    public class DistributorAnalyticsModel
    {
        // ── Overall totals ────────────────────────────────────────────────
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }  // Delivered + Received
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalSpent { get; set; }  // sum of completed orders
        public decimal ThisMonthSpent { get; set; }
        public decimal LastMonthSpent { get; set; }

        // ── Monthly trend (last 6 months) ─────────────────────────────────
        // Label = "Jan 2026", Amount = total spent that month
        public List<MonthlySpend> MonthlyTrend { get; set; } = new();

        // ── Top products (by quantity ordered) ───────────────────────────
        public List<TopProduct> TopProducts { get; set; } = new();

        // ── Order status breakdown ────────────────────────────────────────
        public List<StatusCount> StatusBreakdown { get; set; } = new();

        // ── Computed helpers ─────────────────────────────────────────────
        public decimal CompletionRate =>
            TotalOrders == 0 ? 0 :
            Math.Round((decimal)CompletedOrders / TotalOrders * 100, 1);

        public decimal MonthOnMonthChange =>
            LastMonthSpent == 0 ? 0 :
            Math.Round((ThisMonthSpent - LastMonthSpent) / LastMonthSpent * 100, 1);
    }

    public class MonthlySpend
    {
        public string Label { get; set; } = "";  // "Jan 2026"
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class TopProduct
    {
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal TotalQty { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class StatusCount
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
    }
}