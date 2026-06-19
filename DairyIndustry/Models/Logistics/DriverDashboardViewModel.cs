using DairyIndustry.Models.Admin;

namespace DairyIndustry.Models.Logistics
{
    public class DriverDashboardViewModel
    {
        public DriversModel Driver { get; set; }
        public List<MilkTransferModel> Transfers { get; set; } = new();
        public List<VehiclesModel> Vehicles { get; set; } = new();
        public DriverLossSummary LossSummary { get; set; } = new();
        public List<DriverNotification> Notifications { get; set; } = new();
    }

    public class DriverLossSummary
    {
        public int TotalLossEvents { get; set; }
        public decimal TotalLossLitres { get; set; }
        public decimal AvgLossPct { get; set; }
        public decimal MaxSingleLoss { get; set; }
        public int SevereCount { get; set; }
        public int ModerateCount { get; set; }
        public int MinorCount { get; set; }
    }

    public class DriverNotification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
