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

        // ── FEATURE 4 — Work Anniversaries this month ──────────────
        // Staff whose DOJ month = current month but year != this year.
        // Computed in controller from GetAllStaff() — zero new DB calls.
        public List<StaffModel> AnniversariesThisMonth { get; set; } = new();

        // ── FEATURE 4 — New joinings this month (with names) ────────
        // Staff whose DOJ month AND year = current month/year.
        // Different from Summary.NewJoiningThisMonth which is count only.
        public List<StaffModel> NewJoiningsThisMonth { get; set; } = new();
    }
}