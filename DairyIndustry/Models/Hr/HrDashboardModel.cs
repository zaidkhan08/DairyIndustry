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

        // Feature 4A — Work anniversaries this month
        public List<StaffModel> AnniversariesThisMonth { get; set; } = new();

        // Feature 4B — New joinings this month
        public List<StaffModel> NewJoiningsThisMonth { get; set; } = new();

        // ── FEATURE 2 — Inactive Staff Alert ───────────────────────
        // All staff with IsActive = false.
        // Computed in controller from existing GetAllStaff() call —
        // zero new DB queries needed.
        public List<StaffModel> InactiveStaffList { get; set; } = new();
    }
}