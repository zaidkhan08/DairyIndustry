namespace DairyIndustry.Models.Production
{
    public class MilkTransferModel
    {
        public int TransferId { get; set; }
        public int BatchId { get; set; }
        public int VehicleId { get; set; }
        public int PlantId { get; set; }

        // ── Milk type — stored on MilkTransfers, used for correct inventory update ──
        public int MilkTypeId { get; set; }
        public string MilkTypeName { get; set; }            
        // ── Batch quality benchmarks (for deviation hint) ──────────      
        public decimal? BatchAvgFat { get; set; }
        public decimal? BatchAvgCLR { get; set; }

        public decimal DispatchQty { get; set; }
        public decimal? ReceivedQty { get; set; }
        public decimal? LossQty { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        // ── Display fields (from JOINs) ────────────────────────
        public string BatchRef { get; set; }        // "B-1", "B-2" etc.
        public string PlantName { get; set; }
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }
        public string CenterName { get; set; }
        public bool HasQualityTest { get; set; }

        // ── Derived Status (no Status column — use ReceivedDate)
        public string Status => ReceivedDate.HasValue ? "Received" : "Dispatched";
    }

    // ── Lightweight model used only for dropdowns ──────────────
    public class BatchDropdownModel
    {
        public int BatchId { get; set; }
        public string DisplayText { get; set; }   // "B-3 | Center Name | 28-Mar-2026"
    }
}