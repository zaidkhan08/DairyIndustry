namespace DairyIndustry.Models.Admin
{
    public class MilkTransferModel
    {
        public int TransferId { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public decimal DispatchQty { get; set; }
        public decimal? ReceivedQty { get; set; }
        public decimal? LossQty { get; set; }
        public decimal LossPercent { get; set; }
        public string TransferStatus { get; set; }  // Pending | Received

        // Center info
        public int CenterId { get; set; }
        public string CenterName { get; set; }

        // Plant info
        public int PlantId { get; set; }
        public string PlantName { get; set; }

        // Vehicle info
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; }
        public decimal? VehicleCapacity { get; set; }

        // Driver info
        public int? DriverId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }

        // Quality test
        public decimal? TestedFat { get; set; }
        public decimal? TestedCLR { get; set; }
        public DateTime? TestDate { get; set; }

        // Batch info
        public int BatchId { get; set; }
        public string Shift { get; set; }
        public DateTime BatchDate { get; set; }
        public decimal? BatchAvgFat { get; set; }
        public decimal? BatchAvgCLR { get; set; }
    }
}
