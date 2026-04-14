namespace DairyIndustry.Models.Reports
{
    public class CenterPaymentReportModel
    {
        public int CenterPaymentId { get; set; }
        public string? CenterName { get; set; }
        public string? PlantName { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RatePerLiter { get; set; }
        public decimal? TestedFat { get; set; }
        public decimal? TestedCLR { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
