namespace DairyIndustry.Models.Reports
{
    public class AdminReportViewModel
    {
        public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime ToDate { get; set; } = DateTime.Today;
        public int? CenterId { get; set; }
        public int? PlantId { get; set; }
        public int? DistributorId { get; set; }
        public string? ActiveTab { get; set; } = "dashboard";

        // data
        public List<DashboardMetricModel> Dashboard { get; set; } = new();
        public List<SalesReportModel> Sales { get; set; } = new();
        public List<ProductionSummaryModel> Production { get; set; } = new();
        public List<WastageModel> Wastage { get; set; } = new();
        public List<MilkTransferReportModel> Transfers { get; set; } = new();
        public List<StaffPaymentReportModel> StaffPayments { get; set; } = new();
        public List<CenterPaymentReportModel> CenterPayments { get; set; } = new();
        public List<PendingPaymentModel> PendingPayments { get; set; } = new();
    }
}
