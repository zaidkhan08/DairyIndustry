namespace DairyIndustry.Models.Production
{
    public class QualityTestModel
    {
        public int TestId { get; set; }
        public int TransferId { get; set; }
        public decimal? TestedFat { get; set; }
        public decimal? TestedCLR { get; set; }
        public DateTime TestDate { get; set; }

        // ── Display fields (from JOINs) ────────────────────────
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }
        public string PlantName { get; set; }
        public string CenterName { get; set; }
        public DateTime DispatchDate { get; set; }
        public decimal DispatchQty { get; set; }
        public decimal? ReceivedQty { get; set; }

        // ── Batch quality benchmarks (for comparison) ──────────
        public decimal? BatchAvgFat { get; set; }
        public decimal? BatchAvgCLR { get; set; }

        // ── Derived quality verdict ─────────────────────────────
        // Fat variance > 0.5 or CLR variance > 2 → flag as deviated
        public bool IsFatDeviated =>
            BatchAvgFat.HasValue && TestedFat.HasValue &&
            Math.Abs(TestedFat.Value - BatchAvgFat.Value) > 0.5m;

        public bool IsCLRDeviated =>
            BatchAvgCLR.HasValue && TestedCLR.HasValue &&
            Math.Abs(TestedCLR.Value - BatchAvgCLR.Value) > 2m;

        public string QualityResult =>
            (IsFatDeviated || IsCLRDeviated) ? "Deviated" : "Normal";

        public string QualityBadgeClass => QualityResult switch
        {
            "Deviated" => "bg-danger",
            "Normal" => "bg-success",
            _ => "bg-secondary"
        };
    }
}