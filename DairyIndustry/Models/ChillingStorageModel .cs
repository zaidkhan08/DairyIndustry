namespace DairyIndustry.Models
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

        // Computed display property — not from DB, set in repository
        // Values: "Safe" / "Warning" / "Critical" / "Unknown"
        public string TempStatus { get; set; }

        // Computed display property for badge color in view
        // Returns Bootstrap class based on TempStatus
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
    }
}
