namespace DairyIndustry.Models
{
    public class SalesOrderModel
    {
        public int OrderId { get; set; }
        public int DistributorId { get; set; }
        public string? DistributorName { get; set; }
        public string? Location { get; set; }
        public string? ContactNumber { get; set; }
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? OrderStatus { get; set; }

        public List<SalesOrderDetailModel> OrderDetails { get; set; } = new();

        public string StatusBadgeClass => OrderStatus switch
        {
            "Pending" => "warning",
            "Confirmed" => "info",
            "Dispatched" => "primary",
            "Delivered" => "success",
            "Cancelled" => "danger",
            _ => "secondary"
        };

        public string? NextStatus => OrderStatus switch
        {
            "Pending" => "Confirmed",
            "Confirmed" => "Dispatched",
            "Dispatched" => "Delivered",
            _ => null
        };

        public bool CanCancel => OrderStatus == "Pending" || OrderStatus == "Confirmed";
        public bool CanAddItems => OrderStatus == "Pending";
    }
}