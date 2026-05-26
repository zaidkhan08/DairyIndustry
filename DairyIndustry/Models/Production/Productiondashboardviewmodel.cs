using DairyIndustry.Models.Production;
using DairyIndustry.Models.Finance;

namespace DairyIndustry.Models.Production
{
    public class ProductionDashboardViewModel
    {
        // ── KPI Cards ──────────────────────────────────────────
        public int TotalTransfers { get; set; }
        public int PendingTransfers { get; set; }       // Dispatched, not yet received
        public int ReceivedTransfers { get; set; }

        public int TotalBatches { get; set; }
        public int InProgressBatches { get; set; }
        public int CompletedBatches { get; set; }
        public int QCFailedBatches { get; set; }

        public decimal TotalRawMilkStock { get; set; }  // Sum of all inventory
        public int LowStockCount { get; set; }          // Items with qty <= 500

        public int TotalQualityTests { get; set; }
        public int DeviatedTests { get; set; }

        public decimal TotalLossLitres { get; set; }
        public decimal AvgLossPct { get; set; }

        public int PendingCenterPayments { get; set; }
        public decimal TotalPaidToCenters { get; set; }

        public int TotalProductWastage { get; set; }    // count of wastage records
        public decimal TotalMilkProcessWastage { get; set; } // litres

        // ── Recent items for tables ───────────────────────────
        public List<MilkTransferModel> RecentTransfers { get; set; } = new();
        public List<ProductionBatchModel> RecentBatches { get; set; } = new();
        public List<RawMilkInventoryModel> Inventory { get; set; } = new();
        public List<TransferLossLogModel> RecentLosses { get; set; } = new();
        public List<CenterPaymentModel> RecentCenterPayments { get; set; } = new();
    }
}