namespace DairyIndustry.Models.Reports
{
    public class StaffPaymentReportModel
    {
        public int PaymentId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? RoleName { get; set; }
        public string? PlantName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
