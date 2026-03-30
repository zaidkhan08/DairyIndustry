namespace DairyIndustry.Models.Collection
{
 
    public class DashboardViewModel
    {
        public string StaffName { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public int VillageId { get; set; }
        public string VillageName { get; set; }
        public decimal Capacity { get; set; }
        public string Location { get; set; }

        public int BatchId { get; set; }

        public int TotalFarmers { get; set; }
        public decimal TodayMilkTotal { get; set; }
        public string BatchStatus { get; set; }
    }
}
