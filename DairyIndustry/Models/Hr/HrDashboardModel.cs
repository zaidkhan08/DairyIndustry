namespace DairyIndustry.Models
{
    // ── Overall summary (top cards on dashboard) ──
    public class HRDashboardSummaryModel
    {
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int InactiveStaff { get; set; }
        public int NewJoiningThisMonth { get; set; }
        public int PendingPaymentsCount { get; set; }
        public decimal PendingPaymentsAmount { get; set; }
    }

    // ── Per staff type row ──
    public class StaffTypeCountModel
    {
        public string? StaffType { get; set; }
        public int Count { get; set; }
        public int ActiveCount { get; set; }
    }

    // ── Combined model passed to Dashboard view ──
    public class HRDashboardViewModel
    {
        public HRDashboardSummaryModel Summary { get; set; } = new();
        public List<StaffTypeCountModel> StaffByType { get; set; } = new();
        public List<StaffModel> RecentJoinings { get; set; } = new();
        public List<StaffPaymentModel> RecentPayments { get; set; } = new();
    }
}