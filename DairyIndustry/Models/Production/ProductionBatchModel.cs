namespace DairyIndustry.Models.Production
{
    public class ProductionBatchModel
    {
        public int ProductionBatchId { get; set; }
        public int PlantId { get; set; }
        public int ProductId { get; set; }
        public decimal MilkUsedQuantity { get; set; }
        public DateTime ProductionDate { get; set; }
        public string BatchStatus { get; set; }

        // ── Milk type — stored on ProductionBatches so wastage
        //    logging reads it directly (no fallback guessing) ────
        public int MilkTypeId { get; set; }
        public string MilkTypeName { get; set; }

        // ── Display fields (from JOINs) ────────────────────────
        public string PlantName { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string Unit { get; set; }

        // ── Badge color helper ─────────────────────────────────
        public string StatusBadgeClass => BatchStatus switch
        {
            "InProgress" => "bg-warning text-dark",
            "Completed" => "bg-success",
            "QCFailed" => "bg-danger",
            "Cancelled" => "bg-secondary",
            _ => "bg-light text-dark"
        };
    }
}