namespace DairyIndustry.Models.Finance
{
    // ── Summary card shown at the top of Center Wallet ──────────
    public class CenterWalletSummary
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public decimal TotalReceived { get; set; }     // sum of Processed center payments
        public decimal TotalPending { get; set; }      // sum of Pending center payments
        public decimal TotalStaffCost { get; set; }    // sum of all staff payments for this center
        public int TotalTxns { get; set; }             // total center payment transaction count
        public int TotalStaffCount { get; set; }       // distinct staff count at this center

        // ── NEW: Bonus totals pulled from Finance.CenterWallet ──
        public decimal TotalBonusEarned { get; set; }  // sum of BonusAmount across all payments
        public decimal TotalBaseEarned { get; set; }   // sum of BaseAmount across all payments
    }

    // ── Each row in the Center Wallet inflow table ───────────────
    public class CenterWalletTransaction
    {
        public int CenterPaymentId { get; set; }
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public string BatchRef { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RatePerLiter { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; }
        public string BankStatus { get; set; }
        public string TransactionReference { get; set; }
    }

    // ── NEW: One row per CenterPayment — bonus breakdown ─────────
    //   Comes from Finance.CenterWallet (joined with CenterPayments
    //   for status + center/plant names).
    public class CenterWalletEntry
    {
        public int WalletId { get; set; }
        public int CenterPaymentId { get; set; }
        public string CenterName { get; set; }
        public string PlantName { get; set; }
        public string BatchRef { get; set; }           // "T-12 | 01-Apr-2025"
        public decimal ReceivedQty { get; set; }

        // Rate breakdown
        public decimal BonusRatePerLiter { get; set; } // hardcoded 3
        public decimal BaseRatePerLiter { get; set; }  // RatePerLiter - 3
        public decimal FullRatePerLiter { get; set; }  // what was stored in CenterPayments

        // Amount breakdown — the key columns shown in the wallet table
        public decimal BaseAmount { get; set; }        // what center would earn without bonus
        public decimal BonusAmount { get; set; }       // the extra ₹3/L gain
        public decimal TotalEarned { get; set; }       // BaseAmount + BonusAmount

        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        // ── Badge helper ────────────────────────────────────────
        public string StatusPillClass => PaymentStatus switch
        {
            "Processed" => "pill-processed",
            "Pending" => "pill-pending",
            "Failed" => "pill-failed",
            _ => "pill-cancelled"
        };
    }

    // ── Each row in the Staff Cost (outflow) table ───────────────
    public class CenterStaffPayment
    {
        public int PaymentId { get; set; }
        public string StaffName { get; set; }
        public string RoleName { get; set; }
        public decimal MonthlySalary { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; }
    }

    // ── Wrapper passed to the view ───────────────────────────────
    public class CenterWalletViewModel
    {
        public CenterWalletSummary Summary { get; set; }
        public List<CenterWalletTransaction> Transactions { get; set; } = new();
        public List<CenterStaffPayment> StaffPayments { get; set; } = new();

        // ── NEW: Bonus breakdown entries from Finance.CenterWallet
        public List<CenterWalletEntry> WalletEntries { get; set; } = new();
    }
}