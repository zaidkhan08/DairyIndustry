namespace DairyIndustry.Models.Admin
{
    public class FarmerDropdownModel
    {
        public int FarmerId { get; set; }
        public string FarmerName { get; set; }
        public string? FarmerCode { get; set; }
    }

    public class CenterDropdownModel
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
    }
}