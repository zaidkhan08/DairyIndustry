namespace DairyIndustry.Models.ChillingStorage
{
    // Used for: Index page, Details page, Temperature Alerts page
    // Filled by: GetAll query, GetById query, GetStorageByPlant SP, GetTemperatureAlerts query
    public class ChillingStorageModel
    {
        public int StorageId { get; set; }
        public int PlantId { get; set; }
        public string PlantName { get; set; }
        public int? ProductId { get; set; }       // nullable — NULL means Raw Milk
        public string ItemName { get; set; }       // "Raw Milk" or product name from DB
        public string ProductType { get; set; }    // null if raw milk
        public string Unit { get; set; }           // null if raw milk
        public decimal MilkQuantity { get; set; }
        public decimal? Temperature { get; set; }  // nullable — may not always be recorded
        public DateTime StoredDate { get; set; }

        // NEW: Shift — Morning / Afternoon / Night / null
        public string? Shift { get; set; }

        // Computed display property — not from DB, set in repository
        // Values: "Safe" / "Warning" / "Critical" / "Unknown"
        public string TempStatus { get; set; }

        // Computed display property for badge color in view
        public string TempBadgeClass
        {
            get
            {
                return TempStatus switch
                {
                    "Safe" => "success",
                    "Warning" => "warning",
                    "Critical" => "danger",
                    _ => "secondary"
                };
            }
        }

        // Computed: shift icon for display in views
        public string ShiftIcon => Shift switch
        {
            "Morning" => "bi-sunrise",
            "Afternoon" => "bi-sun",
            "Night" => "bi-moon-stars",
            _ => "bi-dash"
        };

        // Computed: shift badge colour
        public string ShiftBadgeStyle => Shift switch
        {
            "Morning" => "background:#fefce8;color:#854d0e;border:1px solid #fde68a;",
            "Afternoon" => "background:#eff6ff;color:#1d4ed8;border:1px solid #bfdbfe;",
            "Night" => "background:#f5f3ff;color:#5b21b6;border:1px solid #ddd6fe;",
            _ => "background:#f1f5f9;color:#64748b;border:1px solid #e2e8f0;"
        };
    }
}