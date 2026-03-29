namespace DairyIndustry.Models
{
    // Used for: Dashboard / module home page
    // Filled by: DashboardSummary query and PlantCapacitySummary query

    // ── Today's overall summary (top cards on dashboard) ──
    public class ChillingDashboardSummaryModel
    {
        public int TotalEntries { get; set; }
        public decimal TotalQuantity { get; set; }
        public int CriticalAlerts { get; set; }
        public int Warnings { get; set; }
        public int SafeCount { get; set; }
        public int UnknownCount { get; set; }
        public DateTime ReportDate { get; set; }
    }

    // ── Per-plant row in the capacity table ──
    public class ChillingPlantCapacityModel
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; }
        public string Location { get; set; }
        public decimal TotalStoredAllTime { get; set; }
        public decimal StoredToday { get; set; }
        public int TotalEntries { get; set; }
        public int AlertCount { get; set; }
    }

    // ── Combined model passed to the Dashboard View ──
    public class ChillingDashboardViewModel
    {
        public ChillingDashboardSummaryModel Summary { get; set; }
        public List<ChillingPlantCapacityModel> PlantCapacity { get; set; }
        public List<ChillingStorageModel> RecentEntries { get; set; }  // last 5 entries
        public List<ChillingStorageModel> ActiveAlerts { get; set; }   // temp > 5°C today
    }

}
