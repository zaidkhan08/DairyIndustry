namespace DairyIndustry.Models.Admin
{
    public class DashboardLogisticsSummary
    {
        public int TotalDrivers { get; set; }
        public int PendingDrivers { get; set; }
        public int TotalVehicles { get; set; }
        public int TripsToday { get; set; }
        public decimal AvgLossPercent { get; set; }
    }
}
