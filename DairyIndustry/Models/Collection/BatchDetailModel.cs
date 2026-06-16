namespace DairyIndustry.Models.Collection
{
    public class BatchDetailModel
    {

        // Header
        public int BatchId { get; set; }
        public string Shift { get; set; }
        public DateTime BatchDate { get; set; }
        public string Status { get; set; }
        public string CenterName { get; set; }

        // KPIs
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? AvgFat { get; set; }
        public decimal? AvgCLR { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }

        // Entries
        public List<BatchEntryModel> Entries { get; set; }
            = new List<BatchEntryModel>();
    }

    public class BatchEntryModel
    {
        public string FarmerName { get; set; }
        public string FarmerCode { get; set; }
        public string MilkType { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Fat { get; set; }
        public decimal? CLR { get; set; }
        public decimal? Rate { get; set; }
        public decimal? Amount { get; set; }
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public string Remarks { get; set; }
    }
}