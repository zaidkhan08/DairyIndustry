namespace DairyIndustry.Models.Collection
{
    public class AllBatchsModel
    {

        public int BatchId { get; set; }
        public string CenterName { get; set; }
        public string Shift { get; set; }
        public DateTime BatchDate { get; set; }

        public decimal TotalQuantity { get; set; }
        public decimal AvgFat { get; set; }
        public decimal AvgCLR { get; set; }

        public string Status { get; set; }

        public int TotalFarmers { get; set; }
        public decimal TotalMilkCollected { get; set; }
        public decimal TotalAmount { get; set; }
    }
}