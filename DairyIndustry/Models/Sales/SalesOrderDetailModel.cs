namespace DairyIndustry.Models
{
    // Line items for an order — filled by usp_Sales_GetOrderDetails
    public class SalesOrderDetailModel
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    // Product dropdown — filled by inline query on Production.Products
    public class ProductSalesModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? Unit { get; set; }
        public decimal MRP { get; set; }

        public string DisplayText => $"{ProductName} ({ProductType} — {Unit})  MRP: ₹{MRP}";
    }
}