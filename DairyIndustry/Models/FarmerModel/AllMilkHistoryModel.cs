namespace DairyIndustry.Models.FarmerModel
{
    public class AllMilkHistoryModel
    {

        public int CollectionId { get; set; }

        public DateTime CollectionDate { get; set; }

        public string Shift { get; set; }

        public string CenterName { get; set; }

        public string MilkTypeName { get; set; }

        public decimal Quantity { get; set; }

        public decimal? AppliedFat { get; set; }

        public decimal? AppliedCLR { get; set; }

        public decimal RatePerLiter { get; set; }

        public decimal Amount { get; set; }

        public string? ReceiptNumber { get; set; }
    }
}