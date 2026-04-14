namespace DairyIndustry.Models.Production
{
    public class TransferLossLogModel
    {
        public int LossLogId { get; set; }
        public int TransferId { get; set; }
        public decimal DispatchQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal LossQty { get; set; }
        public decimal LossPct { get; set; }   // e.g. 3.45 = 3.45%
        public string LossCategory { get; set; }   // Minor | Moderate | Severe
        public DateTime RecordedAt { get; set; }

        // ── Display fields (JOINs) ─────────────────────────────
        public string PlantName { get; set; }
        public string CenterName { get; set; }
        public string Shift { get; set; }
        public DateTime BatchDate { get; set; }
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        // ── Badge helpers ──────────────────────────────────────
        public string CategoryBadgeClass => LossCategory switch
        {
            "Severe" => "bg-danger",
            "Moderate" => "bg-warning text-dark",
            "Minor" => "bg-info text-dark",
            _ => "bg-secondary"
        };

        public string CategoryIcon => LossCategory switch
        {
            "Severe" => "bi-exclamation-octagon-fill",
            "Moderate" => "bi-exclamation-triangle-fill",
            "Minor" => "bi-info-circle-fill",
            _ => "bi-dash-circle"
        };
    }

    // ── Summary model (one row per plant) ──────────────────────
    public class LossSummaryModel
    {
        public string PlantName { get; set; }
        public int TotalLossEvents { get; set; }
        public decimal TotalLossLitres { get; set; }
        public decimal AvgLossPct { get; set; }
        public decimal MaxSingleLoss { get; set; }
        public int SevereCount { get; set; }
        public int ModerateCount { get; set; }
        public int MinorCount { get; set; }
    }
}