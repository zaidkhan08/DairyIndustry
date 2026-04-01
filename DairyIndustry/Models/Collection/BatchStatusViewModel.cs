namespace DairyIndustry.Models.Collection
{
    public class BatchStatusViewModel
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public string Shift { get; set; }
        public string ShiftWindow { get; set; }     // e.g. "06:00 AM – 10:00 AM"
        public string BatchStatus { get; set; }     // Open / Closed / Not Started Yet / Not Created (Missed)
        public int? BatchId { get; set; }
        public DateTime? BatchDate { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal? AvgFat { get; set; }
        public decimal? AvgCLR { get; set; }
    }
}
