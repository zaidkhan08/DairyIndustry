namespace DairyIndustry.Models.Production
{
    public class ProductWastageModel
    {
        public int WastageId { get; set; }
        public int BatchId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string Reason { get; set; }
        public DateTime RecordedDate { get; set; }

        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string Unit { get; set; }
        public string PlantName { get; set; }
        public string BatchStatus { get; set; }
    }

  
    public class BatchForWastageModel
    {
        public int BatchId { get; set; }
        public string DisplayText { get; set; }  
    }
}