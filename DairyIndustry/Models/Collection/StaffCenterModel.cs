namespace DairyIndustry.Models.Collection
{

    /* ============================================================
      1. usp_Dashboard_StaffCenter
      Staff profile + center + location — 1 row
      ============================================================ */
    public class StaffCenterModel
    {
        // Staff
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string Email { get; set; }
        public string StaffPhone { get; set; }
        public string StaffPhoto { get; set; }
        public DateTime? DOJ { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }

        // Center
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public decimal? Capacity { get; set; }

        // Location
        public int VillageId { get; set; }
        public string VillageName { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int StateId { get; set; }
        public string StateName { get; set; }
    }
    /* ============================================================
       2. usp_Dashboard_TodaySummary
       Today's milk totals across both shifts — 1 row
       ============================================================ */
    public class TodaySummaryModel
    {
        // Milk quantity
        public decimal TotalMilkToday { get; set; }
        public decimal MorningQty { get; set; }
        public decimal EveningQty { get; set; }

        // Amount
        public decimal TotalAmountToday { get; set; }
        public decimal MorningAmount { get; set; }
        public decimal EveningAmount { get; set; }

        // Entry counts
        public int TotalEntriesToday { get; set; }
        public int MorningEntries { get; set; }
        public int EveningEntries { get; set; }

        // Quality
        public decimal? AvgFatToday { get; set; }
        public decimal? AvgCLRToday { get; set; }

        // Payments
        public decimal PendingPaymentAmount { get; set; }

        // Rejections
        public int RejectionsToday { get; set; }
        public decimal RejectedQtyToday { get; set; }
        public int MorningRejections { get; set; }
        public int EveningRejections { get; set; }

        public decimal MorningRejectedQty { get; set; }
        public decimal EveningRejectedQty { get; set; }
    }


    /* ============================================================
       3. usp_Dashboard_ShiftStatus
       Morning + Evening batch status — 2 rows
       ============================================================ */
    public class ShiftStatusModel
    {
        public string Shift { get; set; }
        public string ShiftWindow { get; set; }
        public int? BatchId { get; set; }
        public string BatchStatus { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal? AvgFat { get; set; }
        public decimal? AvgCLR { get; set; }
        public int EntryCount { get; set; }
        public int RejectionCount { get; set; }
        public bool IsCurrentShift { get; set; }
        public DateTime BatchDate { get; set; }
    }


    /* ============================================================
       4. usp_Dashboard_Inventory
       Milk inventory by type — N rows
       ============================================================ */
    public class InventoryModel
    {
        public int InventoryId { get; set; }
        public int MilkTypeId { get; set; }
        public string MilkTypeName { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal? CenterCapacity { get; set; }
        public decimal CapacityUsedPct { get; set; }
        public decimal CollectedTodayQty { get; set; }
        public DateTime LastUpdated { get; set; }
    }


    /* ============================================================
       5. usp_Dashboard_FarmerStats
       All farmer counts for the center — 1 row
       ============================================================ */
    public class FarmerStatsModel
    {
        // Registration breakdown
        public int TotalFarmers { get; set; }
        public int ActiveFarmers { get; set; }
        public int InactiveFarmers { get; set; }
        public int PendingApprovals { get; set; }
        public int RejectedFarmers { get; set; }

        // Today's activity
        public int FarmersDeliveredToday { get; set; }

        // Payments
        public int FarmersPendingPayment { get; set; }
        public decimal TotalPendingPaymentAmount { get; set; }
    }

    //Charts 
    public class CollectionTrendModel
    {
        public DateTime CollectionDate { get; set; }
        public decimal TotalQuantity { get; set; }
    }
    public class CollectionRejectionTrendModel
    {
        public DateTime Date { get; set; }
        public decimal CollectedQty { get; set; }
        public decimal RejectedQty { get; set; }
    }
    //rejection piechart
    public class RejectionReasonModel
    {
        public string RejectionReason { get; set; }
        public int RejectionCount { get; set; }
        public string ChartLabel => $"{RejectionReason} ({RejectionCount})";
    }

    //to giev top 5 farmers of month
    public class TopFarmerModel
    {
        public int FarmerId { get; set; }
        public string FarmerName { get; set; }
        public string FarmerCode { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
        public int DaysDelivered { get; set; }
    }


    //payments 
    public class PaymentStatsModel
    {
        public int PendingCount { get; set; }
        public decimal PendingAmount { get; set; }

        public int ProcessedCount { get; set; }
        public decimal ProcessedAmount { get; set; }

        public int FailedCount { get; set; }
        public decimal FailedAmount { get; set; }
    }
    /* ============================================================
       COMPOSITE — holds all 5 results together
       Populate each property from its own SP call in the repository.
       Use this as the single model passed to the Dashboard view.
       ============================================================ */
    public class CollectionAgentDashboardViewModel
    {
        public StaffCenterModel StaffCenter { get; set; }
        public TodaySummaryModel TodaySummary { get; set; }
        public List<ShiftStatusModel> Shifts { get; set; } = new();
        public List<InventoryModel> Inventory { get; set; } = new();
        public FarmerStatsModel FarmerStats { get; set; }
        public List<CollectionTrendModel> CollectionTrend { get; set; } = new();
        public List<CollectionRejectionTrendModel> CollectionVsRejectionTrend { get; set; } = new();
        public List<RejectionReasonModel> RejectionReasons { get; set; } = new();
        public List<TopFarmerModel> TopFarmers { get; set; }

        public PaymentStatsModel PaymentStats { get; set; }
    }
}
