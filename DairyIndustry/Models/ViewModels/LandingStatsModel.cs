namespace DairyIndustry.Models.ViewModels
{
    public class LandingStatsModel
    {
        public int ActiveCenters { get; set; }
        public int ActivePlants { get; set; }

        public int ActiveFarmers { get; set; }
        public int ActiveStaff { get; set; }
        public int ActiveDrivers { get; set; }
        public int ActiveDistributors { get; set; }

        public decimal TodayMilkCollection { get; set; }   
        public decimal TotalMilkThisMonth { get; set; }   
        public int OpenBatches { get; set; }
        public int PendingOrders { get; set; }

        public decimal RevenueThisMonth { get; set; }  
        public decimal PendingPayments { get; set; }   
    }
}
