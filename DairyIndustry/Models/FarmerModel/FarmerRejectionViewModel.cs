namespace DairyIndustry.Models.FarmerModel
{

    public class FarmerRejectionViewModel
    {
        public int RejectionId { get; set; }
        public DateTime RejectionDate { get; set; }
        public string Shift { get; set; }
        public string ShiftWindow { get; set; }
        public string MilkTypeName { get; set; }
        public decimal Quantity { get; set; }
        public decimal? AppliedFat { get; set; }
        public decimal? AppliedCLR { get; set; }
        public string RejectionReason { get; set; }
        public string Remarks { get; set; }
        public string CenterName { get; set; }
        public string RecordedByStaff { get; set; }
    }
}