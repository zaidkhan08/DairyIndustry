namespace DairyIndustry.Models.Admin
{
    public class RateChartModel
    {
        public int RateChartId { get; set; }
        public int MilkTypeId { get; set; }
        public decimal FatFrom { get; set; }
        public decimal FatTo { get; set; }
        public decimal CLRFrom { get; set; }
        public decimal CLRTo { get; set; }
        public decimal RatePerLiter { get; set; }
        public DateTime EffectiveFrom { get; set; }

        // For display
        public string MilkTypeName { get; set; }


    }
}
