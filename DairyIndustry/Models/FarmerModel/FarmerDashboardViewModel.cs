namespace DairyIndustry.Models.FarmerModel
{
    public class FarmerDashboardViewModel
    {
        public decimal TodayMorningQty { get; set; }
        public decimal TodayMorningAmount { get; set; }
        public decimal TodayEveningQty { get; set; }
        public decimal TodayEveningAmount { get; set; }
        public decimal TodayTotalQty { get; set; }
        public decimal TodayTotalAmount { get; set; }
        public decimal MonthTotalQty { get; set; }
        public decimal MonthTotalAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal? LastPaymentAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }


        public int TotalRejectionsLast30Days { get; set; }
        public decimal TotalRejectedQtyLast30Days { get; set; }

    }
}
