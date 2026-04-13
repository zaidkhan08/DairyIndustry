namespace DairyIndustry.Models.Finance
{
    // ── Main list model ─────────────────────────────────────────
    public class CenterPaymentModel
    {
        public int CenterPaymentId { get; set; }
        public int BatchId { get; set; }
        public int CenterId { get; set; }
        public int PlantId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RatePerLiter { get; set; }
        public decimal? TestedFat { get; set; }
        public decimal? TestedCLR { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; }

        // Joined display
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public string BatchRef { get; set; }
        public string BankStatus { get; set; }
        public string TransactionReference { get; set; }

        // The original TransferId (resolved from BatchId+PlantId) — used for Re-pay link
        public int? OriginalTransferId { get; set; }

        public string StatusBadgeClass => PaymentStatus switch
        {
            "Processed" => "cp-pill-processed",
            "Failed" => "cp-pill-failed",
            _ => "cp-pill-pending"
        };
    }

    // ── Dropdown model for transfers awaiting payment ───────────
    // Shown in the "Process Payment" drawer select
    public class TransferForPaymentModel
    {
        public int TransferId { get; set; }
        public string DisplayText { get; set; }   // "T-12 | Anand Center | 500 L | 01-Apr-2025"

        // Auto-fill data returned alongside the dropdown
        public int CenterId { get; set; }
        public int PlantId { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal? TestedFat { get; set; }
        public decimal? TestedCLR { get; set; }
        public int? MilkTypeId { get; set; }
        public int BatchId { get; set; }
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public int? OriginalTransferId { get; set; }

    }
}