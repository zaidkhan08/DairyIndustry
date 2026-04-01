namespace DairyIndustry.Models.Production
{
    public class RawMilkInventoryModel
    {
        public int RawMilkInventoryId { get; set; }
        public int PlantId { get; set; }
        public int MilkTypeId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime LastUpdated { get; set; }

        // ── Display fields (from JOINs) ────────────────────────
        public string PlantName { get; set; }
        public string MilkTypeName { get; set; }

        // ── Stock level indicator ──────────────────────────────
        public string StockBadgeClass => Quantity switch
        {
            <= 0 => "bg-danger",
            <= 500 => "bg-warning text-dark",
            _ => "bg-success"
        };

        public string StockLabel => Quantity switch
        {
            <= 0 => "Empty",
            <= 500 => "Low",
            _ => "Available"
        };
    }
}