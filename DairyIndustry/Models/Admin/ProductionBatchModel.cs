namespace DairyIndustry.Models.Admin
{
    public class ProductionBatchModel
    {
        public int ProductionBatchId { get; set; }
        public DateTime ProductionDate { get; set; }
        public decimal MilkUsedQuantity { get; set; }
        public string BatchStatus { get; set; }  // InProgress | Completed | QCFailed | Cancelled

        // Product info
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string Unit { get; set; }

        // Plant info
        public int PlantId { get; set; }
        public string PlantName { get; set; }
    }
}
