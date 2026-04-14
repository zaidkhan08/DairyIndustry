namespace DairyIndustry.Models.Reports
{
    public class MilkTransferReportModel
    {
        public int TransferId { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public decimal DispatchQty { get; set; }
        public decimal? ReceivedQty { get; set; }
        public decimal? LossQty { get; set; }
        public decimal LossPercent { get; set; }
        public string? TransferStatus { get; set; }
        public string? CenterName { get; set; }
        public string? PlantName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? DriverName { get; set; }
        public decimal? TestedFat { get; set; }
        public decimal? TestedCLR { get; set; }
    }
}
