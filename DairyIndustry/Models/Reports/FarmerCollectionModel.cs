namespace DairyIndustry.Models.Reports
{
    public class FarmerCollectionModel
    {
        public int FarmerId { get; set; }
        public string FarmerName { get; set; }
        public string CenterName { get; set; }
        public string MilkTypeName { get; set; }
        public int TotalEntries { get; set; }
        public decimal TotalQtyLtr { get; set; }
        public decimal AvgFat { get; set; }
        public decimal AvgCLR { get; set; }
        public decimal TotalPayable { get; set; }
    }
}
