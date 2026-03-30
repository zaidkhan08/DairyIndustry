namespace DairyIndustry.Models.Collection
{

    public class CenterInventoryViewModel
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public string MilkTypeName { get; set; }
        public decimal AvailableQuantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
