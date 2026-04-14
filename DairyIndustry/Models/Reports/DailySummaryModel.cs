namespace DairyIndustry.Models.Reports
{
    public class DailySummaryModel
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public DateTime CollectionDate { get; set; }
        public string Shift { get; set; }
        public string MilkTypeName { get; set; }
        public int TotalEntries { get; set; }
        public decimal TotalQuantityLtr { get; set; }
        public decimal AvgFat { get; set; }
        public decimal AvgCLR { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
