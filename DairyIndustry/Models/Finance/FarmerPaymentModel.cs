namespace DairyIndustry.Models.Finance
{
    public class FarmerPaymentModel
    {
        public int PaymentId { get; set; }
        public int CenterId { get; set; }
        public int FarmerId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; }

        // ── Display fields ─────────────────────────────────────
        public string FarmerName { get; set; }
        public string CenterName { get; set; }
        public string BankStatus { get; set; }
        public string TransactionReference { get; set; }

        // ── Bank details ───────────────────────────────────────
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }

        // ── Paid By ────────────────────────────────────────────
        public int? PaidByUserId { get; set; }
        public string PaidBy { get; set; }   // Full name of the Admin / Collection Agent

        // ── Badge helper ───────────────────────────────────────
        public string StatusBadgeClass => PaymentStatus switch
        {
            "Processed" => "bg-success",
            "Pending" => "bg-warning text-dark",
            "Failed" => "bg-danger",
            "Cancelled" => "bg-secondary",
            _ => "bg-light text-dark"
        };
    }

    // ── Used for preview before creating payment ───────────────
    public class FarmerPaymentPreviewModel
    {
        public int FarmerId { get; set; }
        public int CenterId { get; set; }
        public string FarmerName { get; set; }
        public string CenterName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public int UnpaidCollections { get; set; }
    }
}