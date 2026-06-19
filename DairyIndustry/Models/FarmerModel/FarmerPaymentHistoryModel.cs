namespace DairyIndustry.Models.FarmerModel
{
    public class FarmerPaymentHistoryModel
    {
        public int PaymentId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; }
    }
}