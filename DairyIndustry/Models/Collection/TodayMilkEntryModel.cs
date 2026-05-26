namespace DairyIndustry.Models.Collection
{
    public class TodayMilkEntryModel
    {
    

        public string FarmerName { get; set; }

        public string FarmerCode { get; set; }

        public string Shift { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? Fat { get; set; }

        public decimal? CLR { get; set; }

        public decimal? Amount { get; set; }

        public string Status { get; set; }

        public string RejectionReason { get; set; }

        public DateTime EntryTime { get; set; }
    }
}