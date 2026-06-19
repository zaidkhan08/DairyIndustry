using DairyIndustry.Models.Admin;

namespace DairyIndustry.Models.Admin
{
    // ── Existing models used in VM ─────────────────────────────────
    // User, StaffModel, PlantModel, CollectionCenterModel, ProductModel,
    // ProductionBatchModel, MilkTransferModel  (already in your project)

    // ── Chart data DTOs ────────────────────────────────────────────

    public class ChartPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class DashboardFinanceSummary
    {
        public decimal TotalFarmerPaid { get; set; }
        public decimal TotalFarmerPending { get; set; }
        public decimal TotalStaffPaid { get; set; }
        public decimal TotalStaffPending { get; set; }
        public decimal TotalCenterPaid { get; set; }
        public decimal TotalCenterPending { get; set; }
        public decimal TotalSalesRevenue { get; set; }
    }

    public class DashboardCollectionSummary
    {
        public decimal TotalMilkCollected { get; set; }  // litres, all time
        public decimal TodayMilkCollected { get; set; }  // litres today
        public int TotalFarmers { get; set; }
        public int ActiveFarmers { get; set; }
        public int OpenBatches { get; set; }
        public int ClosedBatches { get; set; }
        public int DispatchedBatches { get; set; }
    }

    public class DashboardProductionSummary
    {
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int QCFailed { get; set; }
        public int Cancelled { get; set; }
        public decimal TotalMilkUsed { get; set; }  // litres
    }

    public class DashboardTransferSummary
    {
        public decimal TotalDispatched { get; set; }
        public decimal TotalReceived { get; set; }
        public decimal TotalLoss { get; set; }
        public decimal LossPercent { get; set; }
        public int PendingCount { get; set; }
        public int ReceivedCount { get; set; }
    }

    // ── Main ViewModel ─────────────────────────────────────────────
    public class AdminDashboardViewModel
    {
        // ── Entity lists (unchanged from your controller) ──────────
        public List<User> Users { get; set; } = new();
        public List<StaffModel> Staff { get; set; } = new();
        public List<PlantModel> Plants { get; set; } = new();
        public List<CollectionCenterModel> Centers { get; set; } = new();
        public List<ProductModel> Products { get; set; } = new();
        public List<ProductionBatchModel> Batches { get; set; } = new();
        public List<MilkTransferModel> Transfers { get; set; } = new();

        // ── Aggregated summaries (computed by repo or controller) ──
        public DashboardCollectionSummary Collection { get; set; } = new();
        public DashboardProductionSummary Production { get; set; } = new();
        public DashboardTransferSummary Transfer { get; set; } = new();
        public DashboardFinanceSummary Finance { get; set; } = new();

        // ── Chart series ───────────────────────────────────────────
        // Milk collected per day — last 7 days
        public List<ChartPoint> MilkLast7Days { get; set; } = new();

        // Production batch count by status
        public List<ChartPoint> BatchByStatus { get; set; } = new();

        // Top 5 products by production milk used
        public List<ChartPoint> TopProductsByMilkUsed { get; set; } = new();

        // Transfer dispatch vs received vs loss — last 5 transfers
        public List<ChartPoint> TransferDispatchSeries { get; set; } = new();
        public List<ChartPoint> TransferReceivedSeries { get; set; } = new();
        public List<ChartPoint> TransferLossSeries { get; set; } = new();

        // Payment overview (farmer / staff / center) — paid vs pending
        public List<ChartPoint> PaymentPaidSeries { get; set; } = new();
        public List<ChartPoint> PaymentPendingSeries { get; set; } = new();

        // Orders by status
        public List<ChartPoint> OrdersByStatus { get; set; } = new();

        // ── Quick-access recent rows ───────────────────────────────
        public List<User> RecentUsers { get; set; } = new();
        public List<ProductionBatchModel> RecentBatches { get; set; } = new();
        public List<MilkTransferModel> RecentTransfers { get; set; } = new();
    }
}