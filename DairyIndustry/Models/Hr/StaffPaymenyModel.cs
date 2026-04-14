namespace DairyIndustry.Models
{
    // Used for: Payments page, Details page payment history
    // Filled by: GetPaymentsByStaff query, GetAllPayments query
    public class StaffPaymentModel
    {
        public int PaymentId { get; set; }
        public int StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StaffType { get; set; }
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentStatus { get; set; }

        // Computed — Bootstrap badge class based on payment status
        public string PaymentBadgeClass => PaymentStatus switch
        {
            "Processed" => "success",
            "Pending" => "warning",
            "Failed" => "danger",
            "Cancelled" => "secondary",
            _ => "secondary"
        };
    }
}