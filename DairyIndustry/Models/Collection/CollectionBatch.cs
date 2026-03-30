namespace DairyIndustry.Models.Collection
{
 
    public class CollectionBatch
    {
        public int BatchId { get; set; }
        public int CenterId { get; set; }
        public string Shift { get; set; }
        public DateTime BatchDate { get; set; }

        public decimal? TotalQuantity { get; set; }
        public decimal? AvgFat { get; set; }
        public decimal? AvgCLR { get; set; }

        public string Status { get; set; }
    }
}
