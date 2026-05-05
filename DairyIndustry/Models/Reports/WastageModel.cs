namespace DairyIndustry.Models.Reports
{
    public class WastageModel
    {
        public string WastageCategory { get; set; }
        public string PlantName { get; set; }
        public string Item { get; set; }
        public decimal TotalWastage { get; set; }
        public string Unit { get; set; }
        public string WastageType { get; set; }
    }
}
