namespace DairyIndustry.Models.Reports
{
    public class ProductionSummaryModel
    {
        public string PlantName { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public int TotalBatches { get; set; }
        public decimal TotalMilkUsedLtr { get; set; }
        public string BatchStatus { get; set; }
    }
}
