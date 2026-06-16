namespace DairyIndustry.Models.ChillingStorage
{
    // ── Today's overall summary ──
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

    // ── Per-plant capacity row ──
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

    // ── Dashboard ViewModel ──
    public class ChillingDashboardViewModel
    {
        public ChillingDashboardSummaryModel Summary { get; set; }
        public List<ChillingPlantCapacityModel> PlantCapacity { get; set; }
        public List<ChillingStorageModel> RecentEntries { get; set; }
        public List<ChillingStorageModel> ActiveAlerts { get; set; }

        // NEW: 7-day quantity trend per plant for sparklines
        public List<ChillingWeeklyTrendModel> WeeklyTrend { get; set; } = new();
    }

    // ── NEW: One plant's 7-day daily quantity trend ──
    // Used for: Dashboard sparkline widget
    public class ChillingWeeklyTrendModel
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; }
        public decimal TotalWeekQty { get; set; }           // sum for the 7 days
        public int TotalWeekEntries { get; set; }
        public List<ChillingDayPointModel> Days { get; set; } = new();
    }

    // ── One day's data point inside a weekly trend ──
    public class ChillingDayPointModel
    {
        public DateTime Date { get; set; }
        public decimal Quantity { get; set; }
        public int EntryCount { get; set; }
    }

    // ── Daily Report row (grouped by date) ──
    public class ChillingDailyReportModel
    {
        public DateTime StoredDate { get; set; }
        public int TotalEntries { get; set; }
        public decimal TotalQuantity { get; set; }
        public int SafeCount { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public int UnknownCount { get; set; }

        public string DayStatus
        {
            get
            {
                if (CriticalCount > 0) return "Critical";
                if (WarningCount > 0) return "Warning";
                if (SafeCount > 0) return "Safe";
                return "Unknown";
            }
        }

        public string DayStatusBadgeClass => DayStatus switch
        {
            "Critical" => "danger",
            "Warning" => "warning",
            "Safe" => "success",
            _ => "secondary"
        };
    }

    // ── NEW: Product-wise report row (grouped by item) ──
    // Used for: Report.cshtml — By Product tab
    public class ChillingProductReportModel
    {
        public string ItemName { get; set; }
        public string ProductType { get; set; }     // null = Raw Milk
        public string Unit { get; set; }
        public int TotalEntries { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AvgTemperature { get; set; }  // 0 if no temp recorded
        public int AlertCount { get; set; }          // entries where temp > 5
        public decimal AvgQuantityPerEntry { get; set; }

        // Computed
        public string ProductLabel => string.IsNullOrEmpty(ProductType)
            ? "Raw Milk" : $"{ProductType}";

        public string TempStatus
        {
            get
            {
                if (AvgTemperature == 0) return "Unknown";
                if (AvgTemperature > 0 && AvgTemperature < 5) return "Safe";
                if (AvgTemperature <= 8) return "Warning";
                return "Critical";
            }
        }

        public string TempBadgeClass => TempStatus switch
        {
            "Safe" => "success",
            "Warning" => "warning",
            "Critical" => "danger",
            _ => "secondary"
        };
    }

    // ── NEW: Combined Report ViewModel ──
    // Passed to Report.cshtml — holds both tab datasets
    public class ChillingReportViewModel
    {
        // By Date tab
        public List<ChillingDailyReportModel> DailyData { get; set; } = new();

        // By Product tab
        public List<ChillingProductReportModel> ProductData { get; set; } = new();

        // Filter state (passed back to view for filter bar)
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string PlantName { get; set; }

        // Summary helpers used by both tabs
        public int TotalDays => DailyData.Count;
        public int TotalEntries => DailyData.Sum(r => r.TotalEntries);
        public decimal TotalQty => DailyData.Sum(r => r.TotalQuantity);
        public int TotalAlerts => DailyData.Sum(r => r.WarningCount + r.CriticalCount);
        public int TotalProducts => ProductData.Count;
    }
}