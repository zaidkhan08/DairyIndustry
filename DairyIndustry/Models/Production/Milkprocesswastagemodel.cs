namespace DairyIndustry.Models.Production
{
    public class MilkProcessWastageModel
    {
        public int WastageId { get; set; }
        public int ProductionBatchId { get; set; }
        public int PlantId { get; set; }
        public int MilkTypeId { get; set; }
        public decimal WastageQuantity { get; set; }
        public string WastageType { get; set; }   // "QCFailed" | "ProcessWastage"
        public string Reason { get; set; }
        public DateTime RecordedDate { get; set; }

        // ── Display fields (from JOINs) ────────────────────────
        public string PlantName { get; set; }
        public string MilkTypeName { get; set; }
        public string BatchStatus { get; set; }
        public DateTime ProductionDate { get; set; }

        // ── Badge helpers ──────────────────────────────────────
        public string WastageTypeBadgeClass => WastageType switch
        {
            "QCFailed" => "bg-danger",
            "ProcessWastage" => "bg-warning text-dark",
            _ => "bg-secondary"
        };

        public string WastageTypeLabel => WastageType switch
        {
            "QCFailed" => "QC Failed",
            "ProcessWastage" => "Process Wastage",
            _ => WastageType
        };
    }
}