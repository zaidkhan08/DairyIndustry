namespace DairyIndustry.Models.Reports
{
    public class SalesReportModel
    {
        public string DistributorName { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
