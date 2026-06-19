namespace DairyIndustry.Models.Collection
{ 
    public class AllMilkEntriesModel
    {
        public DateTime CollectionDate { get; set; }
        public int CollectionId { get; set; }
        public string FarmerName { get; set; }
        public string FarmerCode { get; set; }
        public string Shift { get; set; }
        public string MilkType {  get; set; }   
        public decimal? Quantity { get; set; }
        public decimal? AppliedFat { get; set; }
        public decimal? AppliedCLR { get; set; }
        public decimal? RatePerLiter { get; set; }
        public decimal? Amount { get; set; }
        public int BatchId { get; set; }
        public string Status { get; set; }       
        public string RejectionReason { get; set; }
        public string Remarks { get; set; }

    }
}
