using System.ComponentModel.DataAnnotations;

namespace DairyIndustry.Models.Admin
{
    public class AdminOrderListModel
    {
        // Filter inputs
        public int? DistributorId { get; set; }
        public string? OrderStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Results
        public List<OrderSummary> Orders { get; set; } = new();
        public List<Distributor> DistributorList { get; set; } = new();

        public static readonly List<string> StatusOptions = new()
        {
            "Pending", "Confirmed", "Dispatched", "Delivered", "Cancelled"
        };
    }

    public class OrderSummary
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string DistributorName { get; set; } = "";
        public string? Location { get; set; }
        public string? ContactNumber { get; set; }
    }
}