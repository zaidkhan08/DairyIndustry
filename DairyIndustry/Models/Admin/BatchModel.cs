namespace DairyIndustry.Models.Admin
{
    public class BatchModel
    {
        public int BatchId { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public string Shift { get; set; }
        public DateTime BatchDate { get; set; }
        public decimal? TotalQuantity { get; set; }
        public decimal? AvgFat { get; set; }
        public decimal? AvgCLR { get; set; }
        public string Status { get; set; }

        public string StatusBadgeClass => Status switch
        {
            "Open" => "badge-open",
            "Closed" => "badge-closed",
            "Dispatched" => "badge-dispatched",
            "Cancelled" => "badge-cancelled",
            _ => "badge-secondary"
        };
    }

    public class BatchCollectionEntryModel
    {
        public int CollectionId { get; set; }
        public DateTime CollectionDate { get; set; }
        public string Shift { get; set; }
        public string FarmerName { get; set; }
        public string MilkTypeName { get; set; }
        public decimal Quantity { get; set; }
        public decimal? AppliedFat { get; set; }
        public decimal? AppliedCLR { get; set; }
        public decimal? RatePerLiter { get; set; }
        public decimal? Amount { get; set; }
        public string ReceiptNumber { get; set; }
    }
}
