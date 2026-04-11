namespace DairyIndustry.Models.Reports
{
    public class PendingPaymentModel
    {
        public string? PaymentType { get; set; }
        public int PaymentId { get; set; }
        public string? Payee { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
