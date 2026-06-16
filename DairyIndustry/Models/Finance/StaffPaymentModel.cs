namespace DairyIndustry.Models.Finance
{
    // ── One row in the Staff Payments list ──────────────────────
    public class StaffPaymentModel
    {
        public int PaymentId { get; set; }
        public int PlantId { get; set; }
        public int StaffId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; }

        // ── Joined display fields ──────────────────────────────
        public string StaffName { get; set; }
        public string RoleName { get; set; }
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public decimal? MonthlySalary { get; set; }

        // ── Bank / transaction details ─────────────────────────
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string BankStatus { get; set; }
        public string TransactionReference { get; set; }

        // ── Paid By ────────────────────────────────────────────
        public int? PaidByUserId { get; set; }
        public string PaidBy { get; set; }

        // ── Badge helpers ──────────────────────────────────────
        public string StatusBadgeClass => PaymentStatus switch
        {
            "Processed" => "sp-pill-processed",
            "Failed" => "sp-pill-failed",
            "Cancelled" => "sp-pill-cancelled",
            _ => "sp-pill-pending"
        };
    }

    // ── Dropdown item for the "Pay Staff" drawer ────────────────
    public class StaffForPaymentModel
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public int PlantId { get; set; }
        public int? CenterId { get; set; }
        public decimal? Salary { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public bool HasBankAccount { get; set; }

        // "SP-0021 | Ramesh Patel | ₹12,000 | Anand Center"
        public string DisplayText =>
            $"{FullName} | ₹{Salary?.ToString("N0") ?? "N/A"} | {CenterName}";
    }

    // ── Summary card shown at the top of the list ───────────────
    public class StaffPaymentSummary
    {
        public int TotalPayments { get; set; }
        public int ProcessedCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal TotalAmountPending { get; set; }
        public string ScopeLabel { get; set; }   // "Plant Name" or "All Plants"
    }

    // ── Wrapper passed to list view ──────────────────────────────
    public class StaffPaymentViewModel
    {
        public StaffPaymentSummary Summary { get; set; }
        public List<StaffPaymentModel> Payments { get; set; } = new();
    }
}